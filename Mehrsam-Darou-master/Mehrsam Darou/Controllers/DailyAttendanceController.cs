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
    public class DailyAttendanceController : BaseController
    {
        private readonly DarouAppContext _context;

        public DailyAttendanceController(DarouAppContext context) : base(context)
        {
            _context = context;
        }

        // GET: DailyAttendance/DailyAttendanceList
        public async Task<IActionResult> DailyAttendanceList(int? page, string searchKey, string status, string month)
        {
            var setting = await ReadSettingAsync(_context);
            int pageSize = Convert.ToInt32(setting.NumberPerPage ?? 10);
            int pageNumber = page ?? 1;

            IQueryable<DailyAttendance> query = _context.DailyAttendances
                .Include(d => d.User);

            // Search filter
            if (!string.IsNullOrWhiteSpace(searchKey))
            {
                query = query.Where(d => d.User.FirstName.Contains(searchKey) ||
                                     d.User.LastName.Contains(searchKey) ||
                                     d.User.Username.Contains(searchKey) ||
                                     d.PersianDate.Contains(searchKey));
            }

            // Status filter
            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(d => d.Status == status);
            }

            // Month filter (Persian date)
            if (!string.IsNullOrWhiteSpace(month))
            {
                query = query.Where(d => d.PersianDate.StartsWith(month));
            }

            // Order by most recent first
            query = query.OrderByDescending(d => d.Date);

            int total = await query.CountAsync();
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var paginatedList = new PaginatedList<DailyAttendance>(items, total, pageNumber, pageSize);

            // Pass filter values to view
            ViewBag.SearchKey = searchKey;
            ViewBag.Status = status;
            ViewBag.Month = month;

            return View(paginatedList);
        }

        // GET: DailyAttendance/AddDailyAttendance
        public async Task<IActionResult> AddDailyAttendance()
        {
            ViewBag.Users = await _context.Users
                .Where(u => u.TeamId != null)
                .Select(u => new SelectListItem
                {
                    Value = u.Id.ToString(),
                    Text = u.FirstName + " " + u.LastName
                })
                .ToListAsync();

            return View(new DailyAttendance
            {
                Date = DateOnly.FromDateTime(DateTime.Today),
                Status = "Present",
                IsWorkingDay = true
            });
        }

        // POST: DailyAttendance/AddDailyAttendance
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddDailyAttendance(DailyAttendance dailyAttendance)
        {
            // Remove User from ModelState validation since it's a navigation property
            ModelState.Remove("User");

            // Check if UserId is provided
            if (dailyAttendance.UserId == Guid.Empty)
            {
                ModelState.AddModelError("UserId", "لطفا کاربر را انتخاب کنید");
            }

            // Check for duplicate entry (same user and date)
            var existingEntry = await _context.DailyAttendances
                .FirstOrDefaultAsync(d => d.UserId == dailyAttendance.UserId && d.Date == dailyAttendance.Date);

            if (existingEntry != null)
            {
                ModelState.AddModelError("Date", "برای این کاربر در این تاریخ قبلاً رکورد ثبت شده است");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    dailyAttendance.Id = Guid.NewGuid();
                    dailyAttendance.DateCreated = DateTime.Now;

                    // Set Persian date if not provided
                    if (string.IsNullOrEmpty(dailyAttendance.PersianDate))
                    {
                        var persianCalendar = new System.Globalization.PersianCalendar();
                        var dateTime = dailyAttendance.Date.ToDateTime(TimeOnly.MinValue);
                        dailyAttendance.PersianDate = $"{persianCalendar.GetYear(dateTime)}/{persianCalendar.GetMonth(dateTime):00}/{persianCalendar.GetDayOfMonth(dateTime):00}";
                    }

                    // Create a new DailyAttendance without navigation property
                    var newAttendance = new DailyAttendance
                    {
                        Id = dailyAttendance.Id,
                        UserId = dailyAttendance.UserId,
                        Date = dailyAttendance.Date,
                        PersianDate = dailyAttendance.PersianDate,
                        FirstEntryTime = dailyAttendance.FirstEntryTime,
                        LastExitTime = dailyAttendance.LastExitTime,
                        TotalHours = dailyAttendance.TotalHours,
                        Status = dailyAttendance.Status,
                        IsWorkingDay = dailyAttendance.IsWorkingDay,
                        DateCreated = dailyAttendance.DateCreated
                    };

                    _context.DailyAttendances.Add(newAttendance);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "حضور روزانه با موفقیت ثبت شد";
                    return RedirectToAction(nameof(DailyAttendanceList));
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "خطا در ثبت حضور روزانه: " + ex.Message;
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

            return View(dailyAttendance);
        }

        // GET: DailyAttendance/EditDailyAttendance/5
        public async Task<IActionResult> EditDailyAttendance(Guid id)
        {
            var dailyAttendance = await _context.DailyAttendances
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (dailyAttendance == null)
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

            return View(dailyAttendance);
        }

        // POST: DailyAttendance/EditDailyAttendance/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditDailyAttendance(Guid id, DailyAttendance dailyAttendance)
        {
            if (id != dailyAttendance.Id)
            {
                return NotFound();
            }

            // Remove User from ModelState validation since it's a navigation property
            ModelState.Remove("User");

            // Check if UserId is provided
            if (dailyAttendance.UserId == Guid.Empty)
            {
                ModelState.AddModelError("UserId", "لطفا کاربر را انتخاب کنید");
            }

            // Check for duplicate entry (same user and date, but different ID)
            var existingEntry = await _context.DailyAttendances
                .FirstOrDefaultAsync(d => d.UserId == dailyAttendance.UserId &&
                                         d.Date == dailyAttendance.Date &&
                                         d.Id != id);

            if (existingEntry != null)
            {
                ModelState.AddModelError("Date", "برای این کاربر در این تاریخ قبلاً رکورد دیگری ثبت شده است");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingAttendance = await _context.DailyAttendances.FindAsync(id);
                    if (existingAttendance == null)
                    {
                        return NotFound();
                    }

                    // Keep the original creation date
                    dailyAttendance.DateCreated = existingAttendance.DateCreated;

                    // Update Persian date
                    var persianCalendar = new System.Globalization.PersianCalendar();
                    var dateTime = dailyAttendance.Date.ToDateTime(TimeOnly.MinValue);
                    dailyAttendance.PersianDate = $"{persianCalendar.GetYear(dateTime)}/{persianCalendar.GetMonth(dateTime):00}/{persianCalendar.GetDayOfMonth(dateTime):00}";

                    _context.Entry(existingAttendance).CurrentValues.SetValues(dailyAttendance);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "اطلاعات حضور روزانه با موفقیت به‌روزرسانی شد";
                    return RedirectToAction(nameof(DailyAttendanceList));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DailyAttendanceExists(dailyAttendance.Id))
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

            return View(dailyAttendance);
        }

        // POST: DailyAttendance/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var dailyAttendance = await _context.DailyAttendances
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (dailyAttendance == null)
            {
                TempData["ErrorMessage"] = "رکورد حضور روزانه مورد نظر یافت نشد";
                return RedirectToAction(nameof(DailyAttendanceList));
            }

            try
            {
                _context.DailyAttendances.Remove(dailyAttendance);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "رکورد حضور روزانه با موفقیت حذف شد";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "خطا در حذف رکورد: " + ex.Message;
            }

            return RedirectToAction(nameof(DailyAttendanceList));
        }

        // GET: DailyAttendance/Details/5
        public async Task<IActionResult> Details(Guid id)
        {
            var dailyAttendance = await _context.DailyAttendances
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (dailyAttendance == null)
            {
                return NotFound();
            }

            return View(dailyAttendance);
        }

        private bool DailyAttendanceExists(Guid id)
        {
            return _context.DailyAttendances.Any(e => e.Id == id);
        }
    }
}