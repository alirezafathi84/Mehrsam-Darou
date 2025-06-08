using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mehrsam_Darou.Models;
using Mehrsam_Darou.Services;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Globalization;
using static Mehrsam_Darou.Helper.Helper;

namespace Mehrsam_Darou.Controllers
{
    public class AttendanceLogController : BaseController
    {
        private readonly DarouAppContext _context;
        private readonly DailyAttendanceService _dailyAttendanceService;

        public AttendanceLogController(DarouAppContext context) : base(context)
        {
            _context = context;
            _dailyAttendanceService = new DailyAttendanceService(context);
        }

        // GET: AttendanceLog/AttendanceLogList - Main listing action
        public async Task<IActionResult> AttendanceLogList(int? page, string searchKey, DateTime? startDate, DateTime? endDate)
        {
            var setting = await ReadSettingAsync(_context);
            int pageSize = Convert.ToInt32(setting.NumberPerPage ?? 10);
            int pageNumber = page ?? 1;

            IQueryable<AttendanceLog> query = _context.AttendanceLogs
                .Include(a => a.User);

            // Apply filters
            if (!string.IsNullOrWhiteSpace(searchKey))
            {
                query = query.Where(a => a.User.FirstName.Contains(searchKey) ||
                                        a.User.LastName.Contains(searchKey) ||
                                        a.User.Username.Contains(searchKey) ||
                                        a.LogType.Contains(searchKey));
            }

            if (startDate.HasValue)
            {
                query = query.Where(a => a.LogTime.Date >= startDate.Value.Date);
            }

            if (endDate.HasValue)
            {
                query = query.Where(a => a.LogTime.Date <= endDate.Value.Date);
            }

            query = query.OrderByDescending(a => a.LogTime);

            int total = await query.CountAsync();
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var paginatedList = new PaginatedList<AttendanceLog>(items, total, pageNumber, pageSize);

            // Load users for quick entry modal
            await LoadViewBagData();

            ViewBag.SearchKey = searchKey;
            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");

            return View(paginatedList);
        }

        // GET: AttendanceLog/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var attendanceLog = await _context.AttendanceLogs
                .Include(a => a.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (attendanceLog == null)
            {
                return NotFound();
            }

            return View(attendanceLog);
        }

        // GET: AttendanceLog/AddAttendanceLog
        public async Task<IActionResult> AddAttendanceLog()
        {
            await LoadViewBagData();
            return View(new AttendanceLog
            {
                LogTime = DateTime.Now,
                LogType = "Entry"
            });
        }

        // POST: AttendanceLog/AddAttendanceLog
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAttendanceLog(AttendanceLog attendanceLog)
        {
            // Remove navigation properties from ModelState validation
            ModelState.Remove("User");

            if (ModelState.IsValid)
            {
                try
                {
                    attendanceLog.Id = Guid.NewGuid();
                    attendanceLog.DateCreated = DateTime.Now;
                    attendanceLog.PersianDate = ConvertToPersianDate(attendanceLog.LogTime);

                    _context.Add(attendanceLog);
                    await _context.SaveChangesAsync();

                    // Calculate and update/create DailyAttendance
                    await _dailyAttendanceService.CalculateAndUpdateDailyAttendanceAsync(
                        attendanceLog.UserId, attendanceLog.LogTime.Date);

                    TempData["SuccessMessage"] = "ثبت ورود/خروج با موفقیت انجام شد و حضور روزانه محاسبه شد";
                    return RedirectToAction(nameof(AttendanceLogList));
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "خطا در ثبت اطلاعات: " + ex.Message;
                }
            }

            await LoadViewBagData();
            return View(attendanceLog);
        }

        // GET: AttendanceLog/EditAttendanceLog/5
        public async Task<IActionResult> EditAttendanceLog(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var attendanceLog = await _context.AttendanceLogs.FindAsync(id);
            if (attendanceLog == null)
            {
                return NotFound();
            }

            await LoadViewBagData();
            return View(attendanceLog);
        }

        // POST: AttendanceLog/EditAttendanceLog/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAttendanceLog(Guid id, AttendanceLog attendanceLog)
        {
            if (id != attendanceLog.Id)
            {
                return NotFound();
            }

            // Remove navigation properties from ModelState validation
            ModelState.Remove("User");

            if (ModelState.IsValid)
            {
                try
                {
                    var existingLog = await _context.AttendanceLogs.FindAsync(id);
                    if (existingLog == null)
                    {
                        return NotFound();
                    }

                    var oldDate = existingLog.LogTime.Date;
                    var newDate = attendanceLog.LogTime.Date;

                    // Keep original creation date
                    attendanceLog.DateCreated = existingLog.DateCreated;
                    attendanceLog.PersianDate = ConvertToPersianDate(attendanceLog.LogTime);

                    _context.Entry(existingLog).CurrentValues.SetValues(attendanceLog);
                    await _context.SaveChangesAsync();

                    // Recalculate DailyAttendance for affected dates
                    await _dailyAttendanceService.CalculateAndUpdateDailyAttendanceAsync(attendanceLog.UserId, oldDate);
                    if (oldDate != newDate)
                    {
                        await _dailyAttendanceService.CalculateAndUpdateDailyAttendanceAsync(attendanceLog.UserId, newDate);
                    }

                    TempData["SuccessMessage"] = "اطلاعات ورود/خروج با موفقیت به‌روزرسانی شد و حضور روزانه محاسبه شد";
                    return RedirectToAction(nameof(AttendanceLogList));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AttendanceLogExists(attendanceLog.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            await LoadViewBagData();
            return View(attendanceLog);
        }

        // GET: AttendanceLog/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var attendanceLog = await _context.AttendanceLogs
                .Include(a => a.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (attendanceLog == null)
            {
                return NotFound();
            }

            return View(attendanceLog);
        }

        // POST: AttendanceLog/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var attendanceLog = await _context.AttendanceLogs.FindAsync(id);
            if (attendanceLog == null)
            {
                TempData["ErrorMessage"] = "رکورد ورود/خروج یافت نشد";
                return RedirectToAction(nameof(AttendanceLogList));
            }

            try
            {
                var userId = attendanceLog.UserId;
                var logDate = attendanceLog.LogTime.Date;

                _context.AttendanceLogs.Remove(attendanceLog);
                await _context.SaveChangesAsync();

                // Recalculate DailyAttendance after deletion
                await _dailyAttendanceService.CalculateAndUpdateDailyAttendanceAsync(userId, logDate);

                TempData["SuccessMessage"] = "رکورد ورود/خروج با موفقیت حذف شد و حضور روزانه محاسبه شد";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "خطا در حذف اطلاعات: " + ex.Message;
            }

            return RedirectToAction(nameof(AttendanceLogList));
        }

        // Quick Entry/Exit buttons
        [HttpPost]
        public async Task<IActionResult> QuickEntry(Guid userId)
        {
            try
            {
                var attendanceLog = new AttendanceLog
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    LogTime = DateTime.Now,
                    LogType = "Entry",
                    DateCreated = DateTime.Now,
                    PersianDate = ConvertToPersianDate(DateTime.Now),
                    Location = "سیستم",
                    DeviceId = "WEB"
                };

                _context.AttendanceLogs.Add(attendanceLog);
                await _context.SaveChangesAsync();

                // Calculate daily attendance
                await _dailyAttendanceService.CalculateAndUpdateDailyAttendanceAsync(userId, DateTime.Now.Date);

                TempData["SuccessMessage"] = "ورود با موفقیت ثبت شد";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "خطا در ثبت ورود: " + ex.Message;
            }

            return RedirectToAction(nameof(AttendanceLogList));
        }

        [HttpPost]
        public async Task<IActionResult> QuickExit(Guid userId)
        {
            try
            {
                var attendanceLog = new AttendanceLog
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    LogTime = DateTime.Now,
                    LogType = "Exit",
                    DateCreated = DateTime.Now,
                    PersianDate = ConvertToPersianDate(DateTime.Now),
                    Location = "سیستم",
                    DeviceId = "WEB"
                };

                _context.AttendanceLogs.Add(attendanceLog);
                await _context.SaveChangesAsync();

                // Calculate daily attendance
                await _dailyAttendanceService.CalculateAndUpdateDailyAttendanceAsync(userId, DateTime.Now.Date);

                TempData["SuccessMessage"] = "خروج با موفقیت ثبت شد";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "خطا در ثبت خروج: " + ex.Message;
            }

            return RedirectToAction(nameof(AttendanceLogList));
        }

        // Bulk recalculation method (useful for data migration or corrections)
        [HttpPost]
        public async Task<IActionResult> RecalculateDailyAttendance(DateTime? startDate, DateTime? endDate)
        {
            try
            {
                var start = startDate ?? DateTime.Today.AddDays(-30);
                var end = endDate ?? DateTime.Today;

                var processedDays = await _dailyAttendanceService.RecalculateRangeAsync(start, end);

                TempData["SuccessMessage"] = $"محاسبه مجدد حضور و غیاب برای {processedDays} رکورد انجام شد";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "خطا در محاسبه مجدد: " + ex.Message;
            }

            return RedirectToAction(nameof(AttendanceLogList));
        }

        // Helper method for single user recalculation
        [HttpPost]
        public async Task<IActionResult> RecalculateForUser(Guid userId, DateTime? startDate, DateTime? endDate)
        {
            try
            {
                var start = startDate ?? DateTime.Today.AddDays(-30);
                var end = endDate ?? DateTime.Today;

                await _dailyAttendanceService.RecalculateForUserAsync(userId, start, end);

                var user = await _context.Users.FindAsync(userId);
                TempData["SuccessMessage"] = $"محاسبه مجدد حضور و غیاب برای {user?.FirstName} {user?.LastName} انجام شد";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "خطا در محاسبه مجدد: " + ex.Message;
            }

            return RedirectToAction(nameof(AttendanceLogList));
        }

        // Get attendance statistics for a user
        public async Task<IActionResult> UserStatistics(Guid userId, DateTime? startDate, DateTime? endDate)
        {
            var start = startDate ?? DateTime.Today.AddDays(-30);
            var end = endDate ?? DateTime.Today;

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            var statistics = await _dailyAttendanceService.GetAttendanceStatisticsAsync(userId, start, end);
            var dailyAttendances = await _dailyAttendanceService.GetDailyAttendanceAsync(userId, start, end);

            ViewBag.User = user;
            ViewBag.StartDate = start;
            ViewBag.EndDate = end;
            ViewBag.Statistics = statistics;

            return View(dailyAttendances);
        }

        // API endpoint for getting today's attendance status
        [HttpGet]
        public async Task<IActionResult> GetTodayStatus(Guid userId)
        {
            var today = DateTime.Today;
            var todayLogs = await _context.AttendanceLogs
                .Where(al => al.UserId == userId && al.LogTime.Date == today)
                .OrderBy(al => al.LogTime)
                .ToListAsync();

            var lastEntry = todayLogs.Where(l => l.LogType == "Entry").LastOrDefault();
            var lastExit = todayLogs.Where(l => l.LogType == "Exit").LastOrDefault();

            var status = new
            {
                HasEntry = lastEntry != null,
                HasExit = lastExit != null,
                LastEntryTime = lastEntry?.LogTime.ToString("HH:mm"),
                LastExitTime = lastExit?.LogTime.ToString("HH:mm"),
                IsCurrentlyIn = lastEntry != null && (lastExit == null || lastEntry.LogTime > lastExit.LogTime),
                TotalLogs = todayLogs.Count
            };

            return Json(status);
        }

        private string ConvertToPersianDate(DateTime date)
        {
            var pc = new PersianCalendar();
            return $"{pc.GetYear(date)}/{pc.GetMonth(date):00}/{pc.GetDayOfMonth(date):00}";
        }

        private async Task LoadViewBagData()
        {
            ViewBag.Users = await _context.Users
                .Where(u => u.TeamId != null)
                .OrderBy(u => u.FirstName)
                .ThenBy(u => u.LastName)
                .Select(u => new { u.Id, FullName = u.FirstName + " " + u.LastName })
                .ToListAsync();
        }

        private bool AttendanceLogExists(Guid id)
        {
            return _context.AttendanceLogs.Any(e => e.Id == id);
        }
    }
}