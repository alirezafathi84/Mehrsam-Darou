using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mehrsam_Darou.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Globalization;
using static Mehrsam_Darou.Helper.Helper;

namespace Mehrsam_Darou.Controllers
{
    public class PersianDateConverterController : BaseController
    {
        private readonly DarouAppContext _context;

        public PersianDateConverterController(DarouAppContext context) : base(context)
        {
            _context = context;
        }

        // GET: PersianDateConverter
        public async Task<IActionResult> Index(int? page, string searchKey, int? year, int? month, bool? isHoliday, bool? isWorkingDay)
        {
            var setting = await ReadSettingAsync(_context);
            int pageSize = Convert.ToInt32(setting.NumberPerPage ?? 10);
            int pageNumber = page ?? 1;

            IQueryable<PersianDateConverter> query = _context.PersianDateConverters;

            // Apply filters
            if (!string.IsNullOrWhiteSpace(searchKey))
            {
                query = query.Where(p => p.PersianDate.Contains(searchKey));
            }

            if (year.HasValue)
            {
                query = query.Where(p => p.PersianYear == year.Value);
            }

            if (month.HasValue)
            {
                query = query.Where(p => p.PersianMonth == month.Value);
            }

            if (isHoliday.HasValue)
            {
                query = query.Where(p => p.IsHoliday == isHoliday.Value);
            }

            if (isWorkingDay.HasValue)
            {
                query = query.Where(p => p.IsWorkingDay == isWorkingDay.Value);
            }

            query = query.OrderBy(p => p.GregorianDate);

            int total = await query.CountAsync();
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var paginatedList = new PaginatedList<PersianDateConverter>(items, total, pageNumber, pageSize);

            // Pass filter values to view
            ViewBag.SearchKey = searchKey;
            ViewBag.Year = year;
            ViewBag.Month = month;
            ViewBag.IsHoliday = isHoliday;
            ViewBag.IsWorkingDay = isWorkingDay;

            // Get available years for dropdown
            ViewBag.AvailableYears = await _context.PersianDateConverters
                .Select(p => p.PersianYear)
                .Distinct()
                .OrderBy(y => y)
                .ToListAsync();

            return View(paginatedList);
        }

        // GET: PersianDateConverter/Details/5
        public async Task<IActionResult> Details(DateOnly? gregorianDate)
        {
            if (gregorianDate == null)
            {
                return NotFound();
            }

            var persianDate = await _context.PersianDateConverters
                .FirstOrDefaultAsync(m => m.GregorianDate == gregorianDate.Value);

            if (persianDate == null)
            {
                return NotFound();
            }

            return View(persianDate);
        }

        // GET: PersianDateConverter/Create
        public IActionResult Create()
        {
            var model = new PersianDateConverter
            {
                GregorianDate = DateOnly.FromDateTime(DateTime.Today),
                IsWorkingDay = true,
                IsHoliday = false
            };

            // Calculate Persian date for today
            var pc = new PersianCalendar();
            var today = DateTime.Today;
            model.PersianYear = pc.GetYear(today);
            model.PersianMonth = pc.GetMonth(today);
            model.PersianDay = pc.GetDayOfMonth(today);
            model.PersianDate = $"{model.PersianYear}/{model.PersianMonth:00}/{model.PersianDay:00}";

            return View(model);
        }

        // POST: PersianDateConverter/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PersianDateConverter persianDateConverter)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Check if date already exists
                    var existingDate = await _context.PersianDateConverters
                        .FirstOrDefaultAsync(p => p.GregorianDate == persianDateConverter.GregorianDate);

                    if (existingDate != null)
                    {
                        TempData["ErrorMessage"] = "این تاریخ قبلاً در سیستم ثبت شده است";
                        return View(persianDateConverter);
                    }

                    // Auto-calculate Persian date if not provided
                    if (string.IsNullOrEmpty(persianDateConverter.PersianDate))
                    {
                        var pc = new PersianCalendar();
                        var gregorianDateTime = persianDateConverter.GregorianDate.ToDateTime(TimeOnly.MinValue);
                        persianDateConverter.PersianYear = pc.GetYear(gregorianDateTime);
                        persianDateConverter.PersianMonth = pc.GetMonth(gregorianDateTime);
                        persianDateConverter.PersianDay = pc.GetDayOfMonth(gregorianDateTime);
                        persianDateConverter.PersianDate = $"{persianDateConverter.PersianYear}/{persianDateConverter.PersianMonth:00}/{persianDateConverter.PersianDay:00}";
                    }

                    // Auto-set weekend as holiday if not explicitly set
                    var gregorianDateTime2 = persianDateConverter.GregorianDate.ToDateTime(TimeOnly.MinValue);
                    var dayOfWeek = (int)gregorianDateTime2.DayOfWeek;
                    var isWeekend = dayOfWeek == 4 || dayOfWeek == 5; // Thursday = 4, Friday = 5

                    if (isWeekend && !persianDateConverter.IsHoliday)
                    {
                        persianDateConverter.IsHoliday = true;
                        persianDateConverter.IsWorkingDay = false;
                    }

                    _context.Add(persianDateConverter);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "تاریخ جدید با موفقیت ایجاد شد";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "خطا در ایجاد تاریخ: " + ex.Message;
                }
            }

            return View(persianDateConverter);
        }

        // GET: PersianDateConverter/Edit/5
        public async Task<IActionResult> Edit(DateOnly? gregorianDate)
        {
            if (gregorianDate == null)
            {
                return NotFound();
            }

            var persianDateConverter = await _context.PersianDateConverters
                .FirstOrDefaultAsync(p => p.GregorianDate == gregorianDate.Value);

            if (persianDateConverter == null)
            {
                return NotFound();
            }

            return View(persianDateConverter);
        }

        // POST: PersianDateConverter/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(DateOnly gregorianDate, PersianDateConverter persianDateConverter)
        {
            if (gregorianDate != persianDateConverter.GregorianDate)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingRecord = await _context.PersianDateConverters
                        .FirstOrDefaultAsync(p => p.GregorianDate == gregorianDate);

                    if (existingRecord == null)
                    {
                        return NotFound();
                    }

                    // Update the record
                    existingRecord.PersianDate = persianDateConverter.PersianDate;
                    existingRecord.PersianYear = persianDateConverter.PersianYear;
                    existingRecord.PersianMonth = persianDateConverter.PersianMonth;
                    existingRecord.PersianDay = persianDateConverter.PersianDay;
                    existingRecord.IsHoliday = persianDateConverter.IsHoliday;
                    existingRecord.IsWorkingDay = persianDateConverter.IsWorkingDay;

                    // Ensure consistency: holidays should not be working days
                    if (existingRecord.IsHoliday)
                    {
                        existingRecord.IsWorkingDay = false;
                    }

                    _context.Update(existingRecord);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "تاریخ با موفقیت به‌روزرسانی شد";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PersianDateConverterExists(persianDateConverter.GregorianDate))
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
                    TempData["ErrorMessage"] = "خطا در به‌روزرسانی تاریخ: " + ex.Message;
                }
            }

            return View(persianDateConverter);
        }

        // POST: PersianDateConverter/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(DateOnly gregorianDate)
        {
            var persianDateConverter = await _context.PersianDateConverters
                .FirstOrDefaultAsync(p => p.GregorianDate == gregorianDate);

            if (persianDateConverter == null)
            {
                TempData["ErrorMessage"] = "تاریخ مورد نظر یافت نشد";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                _context.PersianDateConverters.Remove(persianDateConverter);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "تاریخ با موفقیت حذف شد";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "خطا در حذف تاریخ: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        // Bulk operations
        [HttpPost]
        public async Task<IActionResult> GenerateYear(int persianYear)
        {
            try
            {
                // Check if year already exists
                var existingCount = await _context.PersianDateConverters
                    .CountAsync(p => p.PersianYear == persianYear);

                if (existingCount > 0)
                {
                    TempData["ErrorMessage"] = $"سال {persianYear} قبلاً در سیستم موجود است";
                    return RedirectToAction(nameof(Index));
                }

                // Generate dates for the year
                var startDate = PersianToGregorian(persianYear, 1, 1);
                var isLeapYear = IsLeapPersianYear(persianYear);
                var totalDays = isLeapYear ? 366 : 365;

                var dates = new List<PersianDateConverter>();

                for (int day = 0; day < totalDays; day++)
                {
                    var gregorianDate = startDate.AddDays(day);
                    var gregorianDateOnly = DateOnly.FromDateTime(gregorianDate);
                    var (pYear, pMonth, pDay) = GregorianToPersian(gregorianDate);

                    var dayOfWeek = (int)gregorianDate.DayOfWeek;
                    var isWeekend = dayOfWeek == 4 || dayOfWeek == 5; // Thursday = 4, Friday = 5

                    dates.Add(new PersianDateConverter
                    {
                        GregorianDate = gregorianDateOnly,
                        PersianDate = $"{pYear}/{pMonth:00}/{pDay:00}",
                        PersianYear = pYear,
                        PersianMonth = pMonth,
                        PersianDay = pDay,
                        IsHoliday = isWeekend,
                        IsWorkingDay = !isWeekend
                    });
                }

                _context.PersianDateConverters.AddRange(dates);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"سال {persianYear} با {totalDays} روز با موفقیت تولید شد";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "خطا در تولید سال: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> MarkHolidaysInRange(DateTime startDate, DateTime endDate, bool isHoliday)
        {
            try
            {
                var startDateOnly = DateOnly.FromDateTime(startDate);
                var endDateOnly = DateOnly.FromDateTime(endDate);

                var datesInRange = await _context.PersianDateConverters
                    .Where(p => p.GregorianDate >= startDateOnly && p.GregorianDate <= endDateOnly)
                    .ToListAsync();

                foreach (var date in datesInRange)
                {
                    date.IsHoliday = isHoliday;
                    date.IsWorkingDay = !isHoliday;
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"{datesInRange.Count} تاریخ به‌روزرسانی شد";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "خطا در به‌روزرسانی تاریخ‌ها: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        // Helper methods
        private bool PersianDateConverterExists(DateOnly gregorianDate)
        {
            return _context.PersianDateConverters
                .Any(e => e.GregorianDate == gregorianDate);
        }

        private DateTime PersianToGregorian(int persianYear, int persianMonth, int persianDay)
        {
            var pc = new PersianCalendar();
            return pc.ToDateTime(persianYear, persianMonth, persianDay, 0, 0, 0, 0);
        }

        private (int year, int month, int day) GregorianToPersian(DateTime gregorianDate)
        {
            var pc = new PersianCalendar();
            return (pc.GetYear(gregorianDate), pc.GetMonth(gregorianDate), pc.GetDayOfMonth(gregorianDate));
        }

        private bool IsLeapPersianYear(int persianYear)
        {
            var pc = new PersianCalendar();
            return pc.IsLeapYear(persianYear);
        }
    }
}