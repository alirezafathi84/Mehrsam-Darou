using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using Mehrsam_Darou.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using static Mehrsam_Darou.Helper.Helper;

namespace Mehrsam_Darou.Controllers
{
    public class AttendanceLogController : BaseController
    {
        private readonly DarouAppContext _context;

        public AttendanceLogController(DarouAppContext context) : base(context)
        {
            _context = context;
        }

        // GET: AttendanceLog/AttendanceLogList
        public async Task<IActionResult> AttendanceLogList(int? page, string searchKey, string logType, string deviceId)
        {
            var setting = await ReadSettingAsync(_context);
            int pageSize = Convert.ToInt32(setting.NumberPerPage ?? 10);
            int pageNumber = page ?? 1;

            IQueryable<AttendanceLog> query = _context.AttendanceLogs
                .Include(a => a.User);

            // Search filter
            if (!string.IsNullOrWhiteSpace(searchKey))
            {
                query = query.Where(a => a.User.FirstName.Contains(searchKey) ||
                                     a.User.LastName.Contains(searchKey) ||
                                     a.User.Username.Contains(searchKey) ||
                                     a.DeviceId.Contains(searchKey) ||
                                     a.Location.Contains(searchKey));
            }

            // Log type filter
            if (!string.IsNullOrWhiteSpace(logType))
            {
                query = query.Where(a => a.LogType == logType);
            }

            // Device filter
            if (!string.IsNullOrWhiteSpace(deviceId))
            {
                query = query.Where(a => a.DeviceId == deviceId);
            }

            // Order by most recent first
            query = query.OrderByDescending(a => a.LogTime);

            int total = await query.CountAsync();
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var paginatedList = new PaginatedList<AttendanceLog>(items, total, pageNumber, pageSize);

            // Pass filter values to view
            ViewBag.SearchKey = searchKey;
            ViewBag.LogType = logType;
            ViewBag.DeviceId = deviceId;

            return View(paginatedList);
        }

        // GET: AttendanceLog/AddAttendanceLog
        public async Task<IActionResult> AddAttendanceLog()
        {
            ViewBag.Users = await _context.Users
                .Where(u => u.TeamId != null)
                .Select(u => new SelectListItem
                {
                    Value = u.Id.ToString(),
                    Text = u.FirstName + " " + u.LastName
                })
                .ToListAsync();

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
            // Remove User from ModelState validation since it's a navigation property
            ModelState.Remove("User");

            // Debug: Check if UserId is provided
            if (attendanceLog.UserId == Guid.Empty)
            {
                ModelState.AddModelError("UserId", "لطفا کاربر را انتخاب کنید");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    attendanceLog.Id = Guid.NewGuid();
                    attendanceLog.DateCreated = DateTime.Now;

                    // Set Persian date if not provided
                    if (string.IsNullOrEmpty(attendanceLog.PersianDate))
                    {
                        var persianCalendar = new System.Globalization.PersianCalendar();
                        var logDate = attendanceLog.LogTime;
                        attendanceLog.PersianDate = $"{persianCalendar.GetYear(logDate)}/{persianCalendar.GetMonth(logDate):00}/{persianCalendar.GetDayOfMonth(logDate):00}";
                    }

                    // Create a new AttendanceLog without navigation property
                    var newLog = new AttendanceLog
                    {
                        Id = attendanceLog.Id,
                        UserId = attendanceLog.UserId,
                        LogTime = attendanceLog.LogTime,
                        LogType = attendanceLog.LogType,
                        DeviceId = attendanceLog.DeviceId,
                        Location = attendanceLog.Location,
                        PersianDate = attendanceLog.PersianDate,
                        DateCreated = attendanceLog.DateCreated
                    };

                    _context.AttendanceLogs.Add(newLog);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "ثبت حضور و غیاب با موفقیت انجام شد";
                    return RedirectToAction(nameof(AttendanceLogList));
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "خطا در ثبت حضور و غیاب: " + ex.Message;
                }
            }

            // Reload users for dropdown
            ViewBag.Users = await _context.Users
                .Where(u => u.TeamId != null)
                .Select(u => new SelectListItem
                {
                    Value = u.Id.ToString(),
                    Text = u.FirstName + " " + u.LastName
                })
                .ToListAsync();

            return View(attendanceLog);
        }

        // GET: AttendanceLog/EditAttendanceLog/5
        public async Task<IActionResult> EditAttendanceLog(Guid id)
        {
            var attendanceLog = await _context.AttendanceLogs
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (attendanceLog == null)
            {
                return NotFound();
            }

            ViewBag.Users = await _context.Users
                .Where(u => u.TeamId != null)
                .Select(u => new SelectListItem
                {
                    Value = u.Id.ToString(),
                    Text = u.FirstName + " " + u.LastName
                })
                .ToListAsync();

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

            // Remove User from ModelState validation since it's a navigation property
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

                    // Keep the original creation date
                    attendanceLog.DateCreated = existingLog.DateCreated;

                    // Update Persian date if LogTime changed
                    var persianCalendar = new System.Globalization.PersianCalendar();
                    var logDate = attendanceLog.LogTime;
                    attendanceLog.PersianDate = $"{persianCalendar.GetYear(logDate)}/{persianCalendar.GetMonth(logDate):00}/{persianCalendar.GetDayOfMonth(logDate):00}";

                    _context.Entry(existingLog).CurrentValues.SetValues(attendanceLog);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "اطلاعات حضور و غیاب با موفقیت به‌روزرسانی شد";
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
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "خطا در به‌روزرسانی: " + ex.Message;
                }
            }

            // Reload users for dropdown
            ViewBag.Users = await _context.Users
                .Where(u => u.TeamId != null)
                .Select(u => new SelectListItem
                {
                    Value = u.Id.ToString(),
                    Text = u.FirstName + " " + u.LastName
                })
                .ToListAsync();

            return View(attendanceLog);
        }

        // POST: AttendanceLog/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var attendanceLog = await _context.AttendanceLogs
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (attendanceLog == null)
            {
                TempData["ErrorMessage"] = "رکورد حضور و غیاب مورد نظر یافت نشد";
                return RedirectToAction(nameof(AttendanceLogList));
            }

            try
            {
                _context.AttendanceLogs.Remove(attendanceLog);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "رکورد حضور و غیاب با موفقیت حذف شد";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "خطا در حذف رکورد: " + ex.Message;
            }

            return RedirectToAction(nameof(AttendanceLogList));
        }

        // GET: AttendanceLog/Details/5
        public async Task<IActionResult> Details(Guid id)
        {
            var attendanceLog = await _context.AttendanceLogs
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (attendanceLog == null)
            {
                return NotFound();
            }

            return View(attendanceLog);
        }

        private bool AttendanceLogExists(Guid id)
        {
            return _context.AttendanceLogs.Any(e => e.Id == id);
        }
    }
}