using Mehrsam_Darou.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using static Mehrsam_Darou.Helper.Helper;

namespace Mehrsam_Darou.Controllers
{
    public class NotificationController : BaseController
    {
        private readonly DarouAppContext _context;

        public NotificationController(DarouAppContext context) : base(context)
        {
            _context = context;
        }

        // Your existing methods
        public async Task<IActionResult> Noti(Guid notificationId)
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification != null)
            {
                if (notification.Type == "chat")
                {
                    // Mark this specific notification as seen
                    notification.Seen = true;
                    await _context.SaveChangesAsync();

                    // Mark all other chat notifications from the same contact as seen
                    if (notification.RelatedId.HasValue)
                    {
                        var otherChatNotifications = await _context.Notifications
                            .Where(n => n.Type == "chat"
                                       && n.UserId == notification.UserId
                                       && n.RelatedId == notification.RelatedId
                                       && !n.Seen
                                       && n.Id != notificationId)
                            .ToListAsync();

                        if (otherChatNotifications.Any())
                        {
                            foreach (var n in otherChatNotifications)
                                n.Seen = true;
                            await _context.SaveChangesAsync();
                        }
                    }

                    return RedirectToAction("Chat", "Chat", new { contactId = notification.RelatedId });
                }
                else
                {
                    // For non-chat notifications, just mark as seen
                    notification.Seen = true;
                    await _context.SaveChangesAsync();
                }
            }
            return View(notification);
        }

        [HttpPost]
        public async Task<IActionResult> InsertNotification([FromBody] Notification model)
        {
            if (model == null)
                return BadRequest("Invalid notification data.");

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // Create the notification
                    model.Id = Guid.NewGuid();
                    model.CreatedDate = DateTime.Now;
                    model.Seen = false;
                    _context.Notifications.Add(model);

                    // If it's a chat notification, also create a chat message
                    if (model.Type == "chat" && model.UserId.HasValue && model.RelatedId.HasValue)
                    {
                        var chatMessage = new ChatMessage
                        {
                            Id = Guid.NewGuid(),
                            SenderId = model.RelatedId.Value, // RelatedId is the sender
                            ReceiverId = model.UserId.Value,   // UserId is the receiver
                            Content = model.Message,
                            SentAt = DateTime.Now,
                            IsRead = false,
                            Attachments = null
                        };

                        _context.ChatMessages.Add(chatMessage);
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return Ok(model);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return StatusCode(500, $"Internal server error: {ex.Message}");
                }
            }
        }

        [HttpPost]
        public async Task<IActionResult> MarkNotificationSeen(Guid notificationId, string type)
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification == null)
                return NotFound();

            switch (type)
            {
                case "chat":
                    // Mark all chat notifications from the same contact as seen
                    if (notification.RelatedId.HasValue && notification.UserId.HasValue)
                    {
                        var chatNotifications = await _context.Notifications
                            .Where(n => n.Type == "chat"
                                       && n.UserId == notification.UserId
                                       && n.RelatedId == notification.RelatedId
                                       && !n.Seen)
                            .ToListAsync();

                        if (chatNotifications.Any())
                        {
                            foreach (var n in chatNotifications)
                                n.Seen = true;
                            await _context.SaveChangesAsync();
                        }
                    }
                    break;
                default:
                    // For other types, just mark the specific notification as seen
                    notification.Seen = true;
                    await _context.SaveChangesAsync();
                    break;
            }
            return Ok("Notification marked as seen.");
        }

        // GET: Notification/NotificationList
        public async Task<IActionResult> NotificationList(int? page, string searchKey, string type, bool? seen)
        {
            var setting = await ReadSettingAsync(_context);
            int pageSize = Convert.ToInt32(setting.NumberPerPage ?? 10);
            int pageNumber = page ?? 1;

            IQueryable<Notification> query = _context.Notifications
                .Include(n => n.User);

            if (!string.IsNullOrWhiteSpace(searchKey))
            {
                query = query.Where(n => n.Title.Contains(searchKey) ||
                                     n.Message.Contains(searchKey) ||
                                     (n.User != null && ((n.User.FirstName ?? "").Contains(searchKey) || (n.User.LastName ?? "").Contains(searchKey))));
            }

            if (!string.IsNullOrWhiteSpace(type))
            {
                query = query.Where(n => n.Type == type);
            }

            if (seen.HasValue)
            {
                query = query.Where(n => n.Seen == seen.Value);
            }

            query = query.OrderByDescending(n => n.CreatedDate);

            int total = await query.CountAsync();
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var paginatedList = new PaginatedList<Notification>(items, total, pageNumber, pageSize);

            ViewBag.TypeFilter = type;
            ViewBag.SeenFilter = seen;
            return View(paginatedList);
        }

        // GET: Notification/AddNotification
        public async Task<IActionResult> AddNotification()
        {
            await PopulateDropdowns();

            var notification = new Notification
            {
                CreatedDate = DateTime.Now,
                Seen = false,
                Type = "Info"
            };

            return View(notification);
        }

        // POST: Notification/AddNotification
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddNotification(Notification notification, string recipientType = "user", Guid? teamId = null)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var createdDate = DateTime.Now;
                    var createdNotifications = new List<Notification>();

                    // Determine recipients based on recipientType
                    List<User> recipients = new List<User>();

                    switch (recipientType.ToLower())
                    {
                        case "all":
                            // Send to all active users
                            recipients = await _context.Users
                                .Where(u => u.TeamId != null)
                                .ToListAsync();
                            break;

                        case "team":
                            // Send to all users in a specific team
                            var selectedTeamId = teamId ?? notification.UserId;
                            if (selectedTeamId.HasValue)
                            {
                                recipients = await _context.Users
                                    .Where(u => u.TeamId == selectedTeamId)
                                    .ToListAsync();

                                // Debug: Log team selection
                                System.Diagnostics.Debug.WriteLine($"Team ID: {selectedTeamId}, Found users: {recipients.Count}");
                            }
                            break;

                        case "user":
                        default:
                            // Send to specific user
                            if (notification.UserId.HasValue)
                            {
                                var user = await _context.Users.FindAsync(notification.UserId);
                                if (user != null)
                                    recipients.Add(user);
                            }
                            break;
                    }

                    if (!recipients.Any())
                    {
                        string errorMessage = recipientType.ToLower() switch
                        {
                            "all" => "هیچ کاربر فعالی در سیستم یافت نشد",
                            "team" => "هیچ کاربری در تیم انتخاب شده یافت نشد",
                            _ => "کاربر مورد نظر یافت نشد"
                        };

                        TempData["ErrorMessage"] = errorMessage;
                        await PopulateDropdowns();
                        return View(notification);
                    }

                    // Get current logged-in user ID for RelatedId
                    var currentUserId = GetCurrentUserId();
                    if (currentUserId == null)
                    {
                        TempData["ErrorMessage"] = "خطا: کاربر وارد شده شناسایی نشد";
                        await PopulateDropdowns();
                        return View(notification);
                    }

                    // Create notifications for all recipients
                    foreach (var user in recipients)
                    {
                        var userNotification = new Notification
                        {
                            Id = Guid.NewGuid(),
                            RelatedId = currentUserId, // Set to current user's ID
                            Type = notification.Type,
                            Title = notification.Title,
                            Message = notification.Message,
                            UserId = user.Id,
                            Img = notification.Img,
                            CreatedDate = createdDate,
                            Seen = false
                        };

                        createdNotifications.Add(userNotification);
                    }

                    _context.Notifications.AddRange(createdNotifications);
                    await _context.SaveChangesAsync();

                    string successMessage = recipientType.ToLower() switch
                    {
                        "all" => $"اعلان جدید برای {createdNotifications.Count} کاربر با موفقیت ایجاد شد",
                        "team" => $"اعلان جدید برای {createdNotifications.Count} عضو تیم با موفقیت ایجاد شد",
                        _ => "اعلان جدید با موفقیت ایجاد شد"
                    };

                    TempData["SuccessMessage"] = successMessage;
                    return RedirectToAction(nameof(NotificationList));
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "خطا در ایجاد اعلان: " + ex.Message;
                }
            }

            await PopulateDropdowns();
            return View(notification);
        }

        // GET: Notification/EditNotification/5
        public async Task<IActionResult> EditNotification(Guid id)
        {
            var notification = await _context.Notifications
                .Include(n => n.User)
                .FirstOrDefaultAsync(n => n.Id == id);

            if (notification == null)
            {
                return NotFound();
            }

            await PopulateDropdowns();
            return View(notification);
        }

        // POST: Notification/EditNotification/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditNotification(Guid id, Notification notification)
        {
            if (id != notification.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingNotification = await _context.Notifications.FindAsync(id);
                    if (existingNotification == null)
                    {
                        return NotFound();
                    }

                    // Keep the original creation date and user
                    notification.CreatedDate = existingNotification.CreatedDate;
                    notification.UserId = existingNotification.UserId;

                    _context.Entry(existingNotification).CurrentValues.SetValues(notification);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "اعلان با موفقیت به‌روزرسانی شد";
                    return RedirectToAction(nameof(NotificationList));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!NotificationExists(notification.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            await PopulateDropdowns();
            return View(notification);
        }

        // POST: Notification/MarkAsSeen/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsSeen(Guid id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification == null)
            {
                return Json(new { success = false, message = "اعلان یافت نشد" });
            }

            try
            {
                notification.Seen = true;
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "اعلان به عنوان خوانده شده علامت‌گذاری شد" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "خطا در به‌روزرسانی اعلان: " + ex.Message });
            }
        }

        // POST: Notification/MarkAsUnseen/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsUnseen(Guid id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification == null)
            {
                return Json(new { success = false, message = "اعلان یافت نشد" });
            }

            try
            {
                notification.Seen = false;
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "اعلان به عنوان خوانده نشده علامت‌گذاری شد" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "خطا در به‌روزرسانی اعلان: " + ex.Message });
            }
        }

        // POST: Notification/MarkAllAsSeen
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllAsSeen(Guid? userId = null)
        {
            try
            {
                // If userId is not provided, get current user ID
                if (userId == null)
                {
                    userId = GetCurrentUserId();
                }

                if (userId == null)
                {
                    TempData["ErrorMessage"] = "کاربر یافت نشد";
                    return RedirectToAction(nameof(NotificationList));
                }

                var notifications = await _context.Notifications
                    .Where(n => n.UserId == userId && !n.Seen)
                    .ToListAsync();

                foreach (var notification in notifications)
                {
                    notification.Seen = true;
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"{notifications.Count} اعلان به عنوان خوانده شده علامت‌گذاری شد";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "خطا در به‌روزرسانی اعلانات: " + ex.Message;
            }

            return RedirectToAction(nameof(NotificationList));
        }

        // POST: Notification/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification == null)
            {
                TempData["ErrorMessage"] = "اعلان مورد نظر یافت نشد";
                return RedirectToAction(nameof(NotificationList));
            }

            try
            {
                _context.Notifications.Remove(notification);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "اعلان با موفقیت حذف شد";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "خطا در حذف اعلان: " + ex.Message;
            }

            return RedirectToAction(nameof(NotificationList));
        }

        // POST: Notification/DeleteSelected
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSelected(string selectedIds)
        {
            if (string.IsNullOrWhiteSpace(selectedIds))
            {
                TempData["ErrorMessage"] = "هیچ اعلانی انتخاب نشده است";
                return RedirectToAction(nameof(NotificationList));
            }

            try
            {
                var ids = selectedIds.Split(',').Select(Guid.Parse).ToList();
                var notifications = await _context.Notifications.Where(n => ids.Contains(n.Id)).ToListAsync();

                _context.Notifications.RemoveRange(notifications);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"{notifications.Count} اعلان با موفقیت حذف شد";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "خطا در حذف اعلانات: " + ex.Message;
            }

            return RedirectToAction(nameof(NotificationList));
        }

        // GET: Notification/GetUnreadCount
        [HttpGet]
        public async Task<IActionResult> GetUnreadCount(Guid? userId = null)
        {
            try
            {
                // If userId is not provided, get current user ID
                if (userId == null)
                {
                    userId = GetCurrentUserId();
                }

                if (userId == null)
                {
                    return Json(new { count = 0, message = "کاربر یافت نشد" });
                }

                int count = await _context.Notifications
                    .Where(n => n.UserId == userId && !n.Seen)
                    .CountAsync();

                return Json(new { count });
            }
            catch (Exception ex)
            {
                return Json(new { count = 0, message = "خطا در دریافت تعداد اعلانات: " + ex.Message });
            }
        }

        // Helper method to create system notifications
        public async Task<IActionResult> CreateSystemNotification(string title, string message, string type = "System", Guid? userId = null, Guid? relatedId = null)
        {
            try
            {
                var createdDate = DateTime.Now;
                var createdNotifications = new List<Notification>();

                // If relatedId is not provided, use current user ID
                var currentUserId = relatedId ?? GetCurrentUserId();

                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        if (userId == null)
                        {
                            // Create notification for all active users
                            var activeUsers = await _context.Users
                                .Where(u => u.TeamId != null)
                                .ToListAsync();

                            if (type.ToLower() == "chat" && currentUserId.HasValue)
                            {
                                // For chat type, use chat message creation
                                foreach (var user in activeUsers)
                                {
                                    await CreateChatMessage(currentUserId.Value, user.Id, message);
                                }
                            }
                            else
                            {
                                // For non-chat type, create regular notifications
                                foreach (var user in activeUsers)
                                {
                                    var userNotification = new Notification
                                    {
                                        Id = Guid.NewGuid(),
                                        Title = title,
                                        Message = message,
                                        Type = type,
                                        UserId = user.Id,
                                        RelatedId = currentUserId,
                                        CreatedDate = createdDate,
                                        Seen = false
                                    };

                                    createdNotifications.Add(userNotification);
                                }

                                _context.Notifications.AddRange(createdNotifications);
                            }

                            await _context.SaveChangesAsync();
                            await transaction.CommitAsync();

                            return Json(new
                            {
                                success = true,
                                message = type.ToLower() == "chat"
                                    ? $"پیام چت سیستمی برای {activeUsers.Count} کاربر ارسال شد"
                                    : $"اعلان سیستمی برای {activeUsers.Count} کاربر ایجاد شد",
                                notificationIds = createdNotifications.Select(n => n.Id).ToList()
                            });
                        }
                        else
                        {
                            if (type.ToLower() == "chat" && currentUserId.HasValue)
                            {
                                // For chat type, use chat message creation
                                await CreateChatMessage(currentUserId.Value, userId.Value, message);
                            }
                            else
                            {
                                var notification = new Notification
                                {
                                    Id = Guid.NewGuid(),
                                    Title = title,
                                    Message = message,
                                    Type = type,
                                    UserId = userId,
                                    RelatedId = currentUserId,
                                    CreatedDate = createdDate,
                                    Seen = false
                                };

                                _context.Notifications.Add(notification);
                                createdNotifications.Add(notification);
                            }

                            await _context.SaveChangesAsync();
                            await transaction.CommitAsync();

                            return Json(new
                            {
                                success = true,
                                message = type.ToLower() == "chat"
                                    ? "پیام چت سیستمی ارسال شد"
                                    : "اعلان سیستمی ایجاد شد",
                                notificationId = createdNotifications.FirstOrDefault()?.Id
                            });
                        }
                    }
                    catch (Exception)
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "خطا در ایجاد اعلان: " + ex.Message });
            }
        }

        // API method to create notification via AJAX
        [HttpPost]
        public async Task<IActionResult> CreateNotificationApi([FromBody] CreateNotificationModel model)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.Title) || string.IsNullOrWhiteSpace(model.Message))
                {
                    return Json(new { success = false, message = "عنوان و پیام اعلان الزامی است" });
                }

                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                {
                    return Json(new { success = false, message = "خطا: کاربر وارد شده شناسایی نشد" });
                }

                var createdDate = DateTime.Now;
                var recipients = new List<User>();

                if (model.UserId == null)
                {
                    // Get all active users
                    recipients = await _context.Users
                        .Where(u => u.TeamId != null)
                        .ToListAsync();
                }
                else
                {
                    // Get specific user
                    var user = await _context.Users.FindAsync(model.UserId);
                    if (user != null)
                        recipients.Add(user);
                }

                var chatMessagesCreated = 0;
                var notificationsCreated = 0;

                foreach (var user in recipients)
                {
                    // Always create notification
                    var notification = new Notification
                    {
                        Id = Guid.NewGuid(),
                        Title = model.Title,
                        Message = model.Message,
                        Type = model.Type ?? "Info",
                        UserId = user.Id,
                        RelatedId = currentUserId,
                        CreatedDate = createdDate,
                        Seen = false,
                        Img = model.Img
                    };

                    _context.Notifications.Add(notification);
                    notificationsCreated++;

                    // If type is "chat", ALSO create ChatMessage
                    if ((model.Type ?? "Info").ToLower() == "chat")
                    {
                        var chatMessage = new ChatMessage
                        {
                            Id = Guid.NewGuid(),
                            SenderId = currentUserId.Value,
                            ReceiverId = user.Id,
                            Content = model.Message,
                            SentAt = createdDate,
                            IsRead = false,
                            Attachments = null
                        };

                        _context.ChatMessages.Add(chatMessage);
                        chatMessagesCreated++;
                    }
                }

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = (model.Type ?? "Info").ToLower() == "chat"
                        ? $"پیام چت برای {recipients.Count} کاربر ارسال شد"
                        : $"اعلان برای {recipients.Count} کاربر ایجاد شد",
                    chatMessages = chatMessagesCreated,
                    notifications = notificationsCreated
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "خطا در ایجاد اعلان: " + ex.Message });
            }
        }

        // API method to create notification for team
        [HttpPost]
        public async Task<IActionResult> CreateTeamNotificationApi([FromBody] CreateTeamNotificationModel model)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.Title) || string.IsNullOrWhiteSpace(model.Message))
                {
                    return Json(new { success = false, message = "عنوان و پیام اعلان الزامی است" });
                }

                if (model.TeamId == null)
                {
                    return Json(new { success = false, message = "شناسه تیم الزامی است" });
                }

                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                {
                    return Json(new { success = false, message = "خطا: کاربر وارد شده شناسایی نشد" });
                }

                var createdDate = DateTime.Now;
                var teamUsers = await _context.Users
                    .Where(u => u.TeamId == model.TeamId)
                    .ToListAsync();

                if (!teamUsers.Any())
                {
                    return Json(new { success = false, message = "کاربری در این تیم یافت نشد" });
                }

                var chatMessagesCreated = 0;
                var notificationsCreated = 0;

                foreach (var user in teamUsers)
                {
                    // Always create notification
                    var notification = new Notification
                    {
                        Id = Guid.NewGuid(),
                        Title = model.Title,
                        Message = model.Message,
                        Type = model.Type ?? "Info",
                        UserId = user.Id,
                        RelatedId = currentUserId,
                        CreatedDate = createdDate,
                        Seen = false,
                        Img = model.Img
                    };

                    _context.Notifications.Add(notification);
                    notificationsCreated++;

                    // If type is "chat", ALSO create ChatMessage
                    if ((model.Type ?? "Info").ToLower() == "chat")
                    {
                        var chatMessage = new ChatMessage
                        {
                            Id = Guid.NewGuid(),
                            SenderId = currentUserId.Value,
                            ReceiverId = user.Id,
                            Content = model.Message,
                            SentAt = createdDate,
                            IsRead = false,
                            Attachments = null
                        };

                        _context.ChatMessages.Add(chatMessage);
                        chatMessagesCreated++;
                    }
                }

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = (model.Type ?? "Info").ToLower() == "chat"
                        ? $"پیام چت برای {teamUsers.Count} عضو تیم ارسال شد"
                        : $"اعلان برای {teamUsers.Count} عضو تیم ایجاد شد",
                    chatMessages = chatMessagesCreated,
                    notifications = notificationsCreated
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "خطا در ایجاد اعلان: " + ex.Message });
            }
        }

        // Get notifications for current user (API endpoint)
        [HttpGet]
        public async Task<IActionResult> GetUserNotifications(int take = 10, bool unseenOnly = false)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return Json(new { success = false, message = "کاربر یافت نشد" });
                }

                var query = _context.Notifications
                    .Where(n => n.UserId == userId);

                if (unseenOnly)
                {
                    query = query.Where(n => !n.Seen);
                }

                var notifications = await query
                    .OrderByDescending(n => n.CreatedDate)
                    .Take(take)
                    .Select(n => new
                    {
                        n.Id,
                        n.Title,
                        n.Message,
                        n.Type,
                        n.Seen,
                        n.CreatedDate,
                        n.Img,
                        n.RelatedId
                    })
                    .ToListAsync();

                return Json(new { success = true, notifications = notifications });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "خطا در دریافت اعلانات: " + ex.Message });
            }
        }

        private bool NotificationExists(Guid id)
        {
            return _context.Notifications.Any(e => e.Id == id);
        }

        private async Task PopulateDropdowns()
        {
            var users = await _context.Users
                .OrderBy(u => u.FirstName)
                .ToListAsync();

            var teams = await _context.Teams
                .Where(t => t.IsActive == true)
                .OrderBy(t => t.Name)
                .ToListAsync();

            ViewBag.Users = users.Select(u => new {
                Id = u.Id,
                DisplayName = (u.FirstName ?? "") + " " + (u.LastName ?? "")
            }).ToList();

            ViewBag.Teams = teams.Select(t => new {
                Id = t.Id,
                Name = t.Name
            }).ToList();
        }

        // TEST METHOD - Create a simple chat message to verify the model works
        [HttpPost]
        public async Task<IActionResult> TestSimpleChatMessage()
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                {
                    return Json(new { success = false, message = "Current user not found" });
                }

                // Get first other user
                var otherUser = await _context.Users
                    .Where(u => u.Id != currentUserId)
                    .FirstOrDefaultAsync();

                if (otherUser == null)
                {
                    return Json(new { success = false, message = "No other user found for testing" });
                }

                System.Diagnostics.Debug.WriteLine($"TEST: Creating chat message from {currentUserId} to {otherUser.Id}");

                var testMessage = new ChatMessage
                {
                    Id = Guid.NewGuid(),
                    SenderId = currentUserId.Value,
                    ReceiverId = otherUser.Id,
                    Content = "TEST MESSAGE - This is a test chat message",
                    SentAt = DateTime.Now,
                    IsRead = false,
                    Attachments = null
                };

                System.Diagnostics.Debug.WriteLine($"TEST: Adding ChatMessage to context");
                _context.ChatMessages.Add(testMessage);

                System.Diagnostics.Debug.WriteLine($"TEST: Calling SaveChangesAsync");
                var result = await _context.SaveChangesAsync();

                System.Diagnostics.Debug.WriteLine($"TEST: SaveChangesAsync returned: {result}");

                return Json(new
                {
                    success = true,
                    message = $"Test chat message created successfully. Records saved: {result}",
                    messageId = testMessage.Id,
                    senderId = testMessage.SenderId,
                    receiverId = testMessage.ReceiverId
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"TEST ERROR: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"TEST STACK: {ex.StackTrace}");

                return Json(new
                {
                    success = false,
                    message = $"Test failed: {ex.Message}",
                    innerException = ex.InnerException?.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }
        private Guid? GetCurrentUserId()
        {
            try
            {
                // Assuming you store user ID in session
                var userIdString = HttpContext.Session.GetString("UserId");
                if (Guid.TryParse(userIdString, out Guid userId))
                {
                    return userId;
                }

                // Alternative: if using claims
                // var userIdClaim = User.FindFirst("UserId")?.Value;
                // if (Guid.TryParse(userIdClaim, out Guid userId))
                // {
                //     return userId;
                // }

                return null;
            }
            catch
            {
                return null;
            }
        }

        // Helper method to create chat message - modified to not save immediately
        private async Task<ChatMessage> CreateChatMessageInternal(Guid senderId, Guid receiverId, string content)
        {
            var message = new ChatMessage
            {
                Id = Guid.NewGuid(),
                SenderId = senderId,
                ReceiverId = receiverId,
                Content = content,
                Attachments = null,
                SentAt = DateTime.Now,
                IsRead = false
            };

            return message;
        }

        // Helper method to create chat notification - modified to not save immediately  
        private async Task<Notification> CreateChatNotificationInternal(Guid senderId, Guid receiverId, string messageContent)
        {
            // Get sender info
            var sender = await _context.Users.FindAsync(senderId);
            if (sender == null) return null;

            // Create notification
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                Title = $"پیام جدید از {sender.FirstName} {sender.LastName}",
                Message = messageContent.Length > 100 ? messageContent.Substring(0, 100) + "..." : messageContent,
                Type = "chat",
                UserId = receiverId,
                RelatedId = senderId, // The sender's ID for reference
                CreatedDate = DateTime.Now,
                Seen = false
            };

            return notification;
        }

        // Helper method to create chat message - uses the same logic as ChatController
        private async Task CreateChatMessage(Guid senderId, Guid receiverId, string content)
        {
            try
            {
                var message = new ChatMessage
                {
                    Id = Guid.NewGuid(),
                    SenderId = senderId,
                    ReceiverId = receiverId,
                    Content = content,
                    Attachments = null,
                    SentAt = DateTime.Now,
                    IsRead = false
                };

                _context.ChatMessages.Add(message);
                await _context.SaveChangesAsync();

                // Create the notification as well (same as ChatController does)
                await CreateChatNotification(senderId, receiverId, content);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating chat message: {ex.Message}");
                throw;
            }
        }

        // Helper method to create chat notification (same as ChatController)
        private async Task CreateChatNotification(Guid senderId, Guid receiverId, string messageContent)
        {
            try
            {
                // Get sender info
                var sender = await _context.Users.FindAsync(senderId);
                if (sender == null) return;

                // Create notification
                var notification = new Notification
                {
                    Id = Guid.NewGuid(),
                    Title = $"پیام جدید از {sender.FirstName} {sender.LastName}",
                    Message = messageContent.Length > 100 ? messageContent.Substring(0, 100) + "..." : messageContent,
                    Type = "chat",
                    UserId = receiverId,
                    RelatedId = senderId, // The sender's ID for reference
                    CreatedDate = DateTime.Now,
                    Seen = false
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating chat notification: {ex.Message}");
            }
        }
    }

    // Model for API notification creation
    public class CreateNotificationModel
    {
        public string Title { get; set; }
        public string Message { get; set; }
        public string? Type { get; set; }
        public Guid? UserId { get; set; }
        public string? Img { get; set; }
    }

    // Model for team notification creation
    public class CreateTeamNotificationModel
    {
        public string Title { get; set; }
        public string Message { get; set; }
        public string? Type { get; set; }
        public Guid TeamId { get; set; }
        public string? Img { get; set; }
    }
}