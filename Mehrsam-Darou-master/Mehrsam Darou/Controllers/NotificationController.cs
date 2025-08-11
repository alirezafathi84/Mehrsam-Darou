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
                            SenderId = model.RelatedId.Value,
                            ReceiverId = model.UserId.Value,
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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddNotification(Notification notification, string recipientType = "user", Guid? teamId = null)
        {
            if (ModelState.IsValid)
            {
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        var createdDate = DateTime.Now;
                        var currentUserId = GetCurrentUserId();

                        if (currentUserId == null)
                        {
                            TempData["ErrorMessage"] = "User not authenticated";
                            await PopulateDropdowns();
                            return View(notification);
                        }

                        // Get recipients
                        List<User> recipients = recipientType.ToLower() switch
                        {
                            "all" => await _context.Users.Where(u => u.TeamId != null).ToListAsync(),
                            "team" => await _context.Users.Where(u => u.TeamId == (teamId ?? notification.UserId)).ToListAsync(),
                            _ => notification.UserId.HasValue ?
                                new List<User> { await _context.Users.FindAsync(notification.UserId) } :
                                new List<User>()
                        };

                        if (!recipients.Any())
                        {
                            TempData["ErrorMessage"] = "No recipients found";
                            await PopulateDropdowns();
                            return View(notification);
                        }

                        var isChat = notification.Type == "chat";
                        var createdCount = 0;
                        var chatMessagesCreated = 0;

                        foreach (var user in recipients.Where(u => u != null))
                        {
                            // Create notification
                            var userNotification = new Notification
                            {
                                Id = Guid.NewGuid(),
                                RelatedId = currentUserId,
                                Type = notification.Type,
                                Title = notification.Title,
                                Message = notification.Message,
                                UserId = user.Id,
                                Img = notification.Img,
                                CreatedDate = createdDate,
                                Seen = false
                            };
                            _context.Notifications.Add(userNotification);
                            createdCount++;

                            // Create chat message if needed
                            if (isChat)
                            {
                                var chatMessage = new ChatMessage
                                {
                                    Id = Guid.NewGuid(),
                                    SenderId = currentUserId.Value,
                                    ReceiverId = user.Id,
                                    Content = notification.Message,
                                    SentAt = createdDate,
                                    IsRead = false,
                                    Attachments = null
                                };
                                _context.ChatMessages.Add(chatMessage);
                                chatMessagesCreated++;
                            }
                        }

                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();

                        TempData["SuccessMessage"] = isChat ?
                            $"Created {createdCount} notifications and {chatMessagesCreated} chat messages" :
                            $"Created {createdCount} notifications";

                        return RedirectToAction(nameof(NotificationList));
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        TempData["ErrorMessage"] = $"Error: {ex.Message}";
                    }
                }
            }

            await PopulateDropdowns();
            return View(notification);
        }











        [HttpPost]
        public async Task<IActionResult> CreateNotificationApi([FromBody] CreateNotificationModel model)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.Title) || string.IsNullOrWhiteSpace(model.Message))
                {
                    return Json(new { success = false, message = "Title and message are required" });
                }

                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                var recipients = model.UserId == null
                    ? await _context.Users.Where(u => u.TeamId != null).ToListAsync()
                    : new List<User> { await _context.Users.FindAsync(model.UserId) };

                if (!recipients.Any())
                {
                    return Json(new { success = false, message = "No recipients found" });
                }

                var isChat = (model.Type ?? "Info").Equals("chat", StringComparison.OrdinalIgnoreCase);
                var createdDate = DateTime.Now;
                var results = new List<dynamic>();

                foreach (var user in recipients)
                {
                    // Create notification
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

                    // For chat notifications, create chat message
                    if (isChat)
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
                        results.Add(new { NotificationId = notification.Id, ChatMessageId = chatMessage.Id });
                    }
                    else
                    {
                        results.Add(new { NotificationId = notification.Id });
                    }
                }

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = isChat
                        ? $"Chat message and notification sent to {recipients.Count} users"
                        : $"Notification sent to {recipients.Count} users",
                    results
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        private async Task CreateChatMessageAndNotification(Guid senderId, Guid receiverId, string message)
        {
            // Create chat message
            var chatMessage = new ChatMessage
            {
                Id = Guid.NewGuid(),
                SenderId = senderId,
                ReceiverId = receiverId,
                Content = message,
                SentAt = DateTime.Now,
                IsRead = false,
                Attachments = null
            };
            _context.ChatMessages.Add(chatMessage);

            // Get sender info for notification
            var sender = await _context.Users.FindAsync(senderId);
            var notificationTitle = $"New message from {sender?.FirstName} {sender?.LastName}";

            // Create notification
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                Title = notificationTitle,
                Message = message.Length > 100 ? message.Substring(0, 100) + "..." : message,
                Type = "chat",
                UserId = receiverId,
                RelatedId = senderId,
                CreatedDate = DateTime.Now,
                Seen = false
            };
            _context.Notifications.Add(notification);

            await _context.SaveChangesAsync();
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