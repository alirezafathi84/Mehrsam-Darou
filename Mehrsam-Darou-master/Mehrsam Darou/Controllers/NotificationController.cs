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

        // New management methods
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
        public async Task<IActionResult> AddNotification(Notification notification)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    notification.Id = Guid.NewGuid();
                    notification.CreatedDate = DateTime.Now;
                    notification.Seen = false;

                    _context.Add(notification);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "اعلان جدید با موفقیت ایجاد شد";
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

                    // Keep the original creation date
                    notification.CreatedDate = existingNotification.CreatedDate;

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
                IQueryable<Notification> query = _context.Notifications.Where(n => !n.Seen);

                if (userId.HasValue)
                {
                    query = query.Where(n => n.UserId == userId.Value);
                }

                var notifications = await query.ToListAsync();
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
            IQueryable<Notification> query = _context.Notifications.Where(n => !n.Seen);

            if (userId.HasValue)
            {
                query = query.Where(n => n.UserId == userId.Value);
            }

            int count = await query.CountAsync();
            return Json(new { count });
        }

        // Helper method to create system notifications
        public async Task<IActionResult> CreateSystemNotification(string title, string message, string type = "System", Guid? userId = null, Guid? relatedId = null)
        {
            try
            {
                var notification = new Notification
                {
                    Id = Guid.NewGuid(),
                    Title = title,
                    Message = message,
                    Type = type,
                    UserId = userId, // null means it's for all users
                    RelatedId = relatedId,
                    CreatedDate = DateTime.Now,
                    Seen = false
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "اعلان سیستمی ایجاد شد", notificationId = notification.Id });
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

                var notification = new Notification
                {
                    Id = Guid.NewGuid(),
                    Title = model.Title,
                    Message = model.Message,
                    Type = model.Type ?? "Info",
                    UserId = model.UserId,
                    RelatedId = model.RelatedId,
                    CreatedDate = DateTime.Now,
                    Seen = false,
                    Img = model.Img
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "اعلان ایجاد شد", notificationId = notification.Id });
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
                    .Where(n => n.UserId == userId || n.UserId == null);

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

            ViewBag.Users = users.Select(u => new {
                Id = u.Id,
                DisplayName = (u.FirstName ?? "") + " " + (u.LastName ?? "")
            }).ToList();
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
}