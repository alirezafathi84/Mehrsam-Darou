// =============================================
// Financial Controller
// =============================================
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mehrsam_Darou.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using static Mehrsam_Darou.Helper.Helper;

namespace Mehrsam_Darou.Controllers
{
    public class FinancialController : BaseController
    {
        private readonly DarouAppContext _context;

        public FinancialController(DarouAppContext context) : base(context)
        {
            _context = context;
        }

        // GET: Financial/FinancialReport
        public async Task<IActionResult> FinancialReport(int? page, string searchKey, string reportType, string periodType)
        {
            var setting = await ReadSettingAsync(_context);
            int pageSize = Convert.ToInt32(setting.NumberPerPage ?? 10);
            int pageNumber = page ?? 1;

            IQueryable<FinancialReport> query = _context.FinancialReports;

            if (!string.IsNullOrWhiteSpace(searchKey))
            {
                query = query.Where(f => f.ReportName.Contains(searchKey) ||
                                     f.ReportCode.Contains(searchKey))
                            .OrderByDescending(f => f.CreatedDate);
            }

            if (!string.IsNullOrWhiteSpace(reportType))
            {
                query = query.Where(f => f.ReportType == reportType);
            }

            if (!string.IsNullOrWhiteSpace(periodType))
            {
                query = query.Where(f => f.PeriodType == periodType);
            }

            int total = await query.CountAsync();
            var items = await query
                .OrderByDescending(f => f.CreatedDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var paginatedList = new PaginatedList<FinancialReport>(items, total, pageNumber, pageSize);

            ViewBag.ReportTypes = new List<string> { "ترازنامه", "سود و زیان", "گردش نقدینگی", "تغییرات حقوق صاحبان سهام" };
            ViewBag.PeriodTypes = new List<string> { "ماهانه", "فصلی", "سالانه" };

            return View(paginatedList);
        }

        // GET: Financial/AddFinancialReport
        public IActionResult AddFinancialReport()
        {
            ViewBag.ReportTypes = new List<string> { "ترازنامه", "سود و زیان", "گردش نقدینگی", "تغییرات حقوق صاحبان سهام" };
            ViewBag.PeriodTypes = new List<string> { "ماهانه", "فصلی", "سالانه" };

            return View(new FinancialReport
            {
                PeriodStart = DateOnly.FromDateTime(DateTime.Now.Date.AddMonths(-1)),
                PeriodEnd = DateOnly.FromDateTime(DateTime.Now.Date),
                FiscalYear = DateTime.Now.Year,
                CreatedDate = DateTime.Now
            });
        }

        // POST: Financial/AddFinancialReport
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddFinancialReport(FinancialReport financialReport)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    if (await _context.FinancialReports.AnyAsync(f => f.ReportCode == financialReport.ReportCode))
                    {
                        TempData["ErrorMessage"] = "گزارش با این کد قبلاً ثبت شده است";
                        ViewBag.ReportTypes = new List<string> { "ترازنامه", "سود و زیان", "گردش نقدینگی", "تغییرات حقوق صاحبان سهام" };
                        ViewBag.PeriodTypes = new List<string> { "ماهانه", "فصلی", "سالانه" };
                        return View(financialReport);
                    }

                    financialReport.ReportId = Guid.NewGuid();
                    financialReport.CreatedDate = DateTime.Now;
                    financialReport.Status = "در حال تهیه";

                    _context.Add(financialReport);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "گزارش مالی جدید با موفقیت ایجاد شد";
                    return RedirectToAction(nameof(FinancialReport));
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "خطا در ایجاد گزارش مالی: " + ex.Message;
                }
            }

            ViewBag.ReportTypes = new List<string> { "ترازنامه", "سود و زیان", "گردش نقدینگی", "تغییرات حقوق صاحبان سهام" };
            ViewBag.PeriodTypes = new List<string> { "ماهانه", "فصلی", "سالانه" };
            return View(financialReport);
        }

        // GET: Financial/EditFinancialReport/5
        public async Task<IActionResult> EditFinancialReport(Guid id)
        {
            var financialReport = await _context.FinancialReports.FindAsync(id);
            if (financialReport == null)
            {
                return NotFound();
            }

            ViewBag.ReportTypes = new List<string> { "ترازنامه", "سود و زیان", "گردش نقدینگی", "تغییرات حقوق صاحبان سهام" };
            ViewBag.PeriodTypes = new List<string> { "ماهانه", "فصلی", "سالانه" };
            ViewBag.StatusList = new List<string> { "در حال تهیه", "تکمیل شده", "تایید شده" };

            return View(financialReport);
        }

        // POST: Financial/EditFinancialReport/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditFinancialReport(Guid id, FinancialReport financialReport)
        {
            if (id != financialReport.ReportId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingReport = await _context.FinancialReports.FindAsync(id);
                    if (existingReport == null)
                    {
                        return NotFound();
                    }

                    // Keep the original creation date
                    financialReport.CreatedDate = existingReport.CreatedDate;

                    _context.Entry(existingReport).CurrentValues.SetValues(financialReport);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "اطلاعات گزارش مالی با موفقیت به‌روزرسانی شد";
                    return RedirectToAction(nameof(FinancialReport));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!FinancialReportExists(financialReport.ReportId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            ViewBag.ReportTypes = new List<string> { "ترازنامه", "سود و زیان", "گردش نقدینگی", "تغییرات حقوق صاحبان سهام" };
            ViewBag.PeriodTypes = new List<string> { "ماهانه", "فصلی", "سالانه" };
            ViewBag.StatusList = new List<string> { "در حال تهیه", "تکمیل شده", "تایید شده" };

            return View(financialReport);
        }

        // POST: Financial/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var financialReport = await _context.FinancialReports.FindAsync(id);
            if (financialReport == null)
            {
                TempData["ErrorMessage"] = "گزارش مالی مورد نظر یافت نشد";
                return RedirectToAction(nameof(FinancialReport));
            }

            try
            {
                _context.FinancialReports.Remove(financialReport);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "گزارش مالی با موفقیت حذف شد";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "خطا در حذف گزارش مالی: " + ex.Message;
            }

            return RedirectToAction(nameof(FinancialReport));
        }

        private bool FinancialReportExists(Guid id)
        {
            return _context.FinancialReports.Any(e => e.ReportId == id);
        }
    }
}