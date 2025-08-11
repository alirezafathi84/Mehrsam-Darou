using Mehrsam_Darou.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
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
                    await MarkNotificationSeen(notificationId, "chat");
                    return RedirectToAction("Chat", "Chat", new { contactId = notification.RelatedId });
                }
            }
            return View(notification);
        }

        [HttpPost]
        public async Task<IActionResult> InsertNotification([FromBody] Notification model)
        {
            if (model == null)
                return BadRequest("Invalid notification data.");
            model.Id = Guid.NewGuid();
            model.CreatedDate = DateTime.Now;
            model.Seen = false;
            _context.Notifications.Add(model);
            await _context.SaveChangesAsync();
            return Ok(model);
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
                    var notifications = await _context.Notifications
                        .Where(m => m.RelatedId.Equals(notification.RelatedId) && m.Type == "chat" && !m.Seen)
                        .ToListAsync();
                    if (notifications.Any())
                    {
                        foreach (var n in notifications)
                            n.Seen = true;
                        await _context.SaveChangesAsync();
                    }
                    break;
                default:
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

                    // Create notifications for all recipients
                    foreach (var user in recipients)
                    {
                        var userNotification = new Notification
                        {
                            Id = Guid.NewGuid(),
                            RelatedId = notification.RelatedId,
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

                if (userId == null)
                {
                    // Create notification for all active users
                    var activeUsers = await _context.Users
                        .Where(u => u.TeamId != null)
                        .ToListAsync();

                    foreach (var user in activeUsers)
                    {
                        var userNotification = new Notification
                        {
                            Id = Guid.NewGuid(),
                            Title = title,
                            Message = message,
                            Type = type,
                            UserId = user.Id,
                            RelatedId = relatedId,
                            CreatedDate = createdDate,
                            Seen = false
                        };

                        createdNotifications.Add(userNotification);
                    }

                    _context.Notifications.AddRange(createdNotifications);
                    await _context.SaveChangesAsync();

                    return Json(new { success = true, message = $"اعلان سیستمی برای {createdNotifications.Count} کاربر ایجاد شد", notificationIds = createdNotifications.Select(n => n.Id).ToList() });
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
                        RelatedId = relatedId,
                        CreatedDate = createdDate,
                        Seen = false
                    };

                    _context.Notifications.Add(notification);
                    await _context.SaveChangesAsync();

                    return Json(new { success = true, message = "اعلان سیستمی ایجاد شد", notificationId = notification.Id });
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

                var createdDate = DateTime.Now;
                var createdNotifications = new List<Guid>();

                if (model.UserId == null)
                {
                    // Create notification for all active users
                    var activeUsers = await _context.Users
                        .Where(u => u.TeamId != null)
                        .ToListAsync();

                    var notifications = new List<Notification>();

                    foreach (var user in activeUsers)
                    {
                        var userNotificationId = Guid.NewGuid();
                        var userNotification = new Notification
                        {
                            Id = userNotificationId,
                            Title = model.Title,
                            Message = model.Message,
                            Type = model.Type ?? "Info",
                            UserId = user.Id,
                            RelatedId = model.RelatedId,
                            CreatedDate = createdDate,
                            Seen = false,
                            Img = model.Img
                        };

                        notifications.Add(userNotification);
                        createdNotifications.Add(userNotificationId);
                    }

                    _context.Notifications.AddRange(notifications);
                    await _context.SaveChangesAsync();

                    return Json(new
                    {
                        success = true,
                        message = $"اعلان برای {notifications.Count} کاربر ایجاد شد",
                        notificationIds = createdNotifications
                    });
                }
                else
                {
                    // Create notification for specific user
                    var notificationId = Guid.NewGuid();
                    var notification = new Notification
                    {
                        Id = notificationId,
                        Title = model.Title,
                        Message = model.Message,
                        Type = model.Type ?? "Info",
                        UserId = model.UserId,
                        RelatedId = model.RelatedId,
                        CreatedDate = createdDate,
                        Seen = false,
                        Img = model.Img
                    };

                    _context.Notifications.Add(notification);
                    await _context.SaveChangesAsync();

                    return Json(new
                    {
                        success = true,
                        message = "اعلان ایجاد شد",
                        notificationId = notificationId
                    });
                }
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

                var createdDate = DateTime.Now;
                var teamUsers = await _context.Users
                    .Where(u => u.TeamId == model.TeamId)
                    .ToListAsync();

                if (!teamUsers.Any())
                {
                    return Json(new { success = false, message = "کاربری در این تیم یافت نشد" });
                }

                var notifications = new List<Notification>();
                var createdNotifications = new List<Guid>();

                foreach (var user in teamUsers)
                {
                    var userNotificationId = Guid.NewGuid();
                    var userNotification = new Notification
                    {
                        Id = userNotificationId,
                        Title = model.Title,
                        Message = model.Message,
                        Type = model.Type ?? "Info",
                        UserId = user.Id,
                        RelatedId = model.RelatedId,
                        CreatedDate = createdDate,
                        Seen = false,
                        Img = model.Img
                    };

                    notifications.Add(userNotification);
                    createdNotifications.Add(userNotificationId);
                }

                _context.Notifications.AddRange(notifications);
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = $"اعلان برای {notifications.Count} عضو تیم ایجاد شد",
                    notificationIds = createdNotifications
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

        // Helper method to get current user ID
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
    }

    // Model for API notification creation
    public class CreateNotificationModel
    {
        public string Title { get; set; }
        public string Message { get; set; }
        public string? Type { get; set; }
        public Guid? UserId { get; set; }
        public Guid? RelatedId { get; set; }
        public string? Img { get; set; }
    }

    // Model for team notification creation
    public class CreateTeamNotificationModel
    {
        public string Title { get; set; }
        public string Message { get; set; }
        public string? Type { get; set; }
        public Guid TeamId { get; set; }
        public Guid? RelatedId { get; set; }
        public string? Img { get; set; }
    }
}