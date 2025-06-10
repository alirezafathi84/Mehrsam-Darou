using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mehrsam_Darou.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using static Mehrsam_Darou.Helper.Helper;

namespace Mehrsam_Darou.Controllers
{
    public class QAStandardController : BaseController
    {
        private readonly DarouAppContext _context;

        public QAStandardController(DarouAppContext context) : base(context)
        {
            _context = context;
        }

        // GET: QAStandard/QAStandardList
        public async Task<IActionResult> QAStandardList(int? page, string searchKey)
        {
            var setting = await ReadSettingAsync(_context);
            int pageSize = Convert.ToInt32(setting.NumberPerPage ?? 10);
            int pageNumber = page ?? 1;

            IQueryable<QaStandard> query = _context.QaStandards
                .Include(q => q.CreatedByNavigation);

            if (!string.IsNullOrWhiteSpace(searchKey))
            {
                query = query.Where(q => q.StandardCode.Contains(searchKey) ||
                                     q.StandardName.Contains(searchKey) ||
                                     q.IssuingOrganization.Contains(searchKey) ||
                                     q.ResponsibleDepartment.Contains(searchKey))
                            .OrderBy(q => q.StandardName);
            }

            int total = await query.CountAsync();
            var items = await query
                .OrderByDescending(q => q.CreatedDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var paginatedList = new PaginatedList<QaStandard>(items, total, pageNumber, pageSize);

            return View(paginatedList);
        }

        // GET: QAStandard/AddQAStandard
        public IActionResult AddQAStandard()
        {
            return View(new QaStandard
            {
                IsActive = true,
                CreatedDate = DateTime.Now,
                PriorityLevel = 3,
                CompliancePercentage = 0,
                NonConformitiesCount = 0,
                ActionItemsCount = 0,
                IsMandatory = false,
                Currency = "IRR"
            });
        }

        // POST: QAStandard/AddQAStandard
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddQAStandard(QaStandard standard)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    if (await _context.QaStandards.AnyAsync(q => q.StandardCode == standard.StandardCode))
                    {
                        TempData["ErrorMessage"] = "استاندارد با این کد قبلاً ثبت شده است";
                        return View(standard);
                    }

                    standard.StandardId = Guid.NewGuid();
                    standard.CreatedDate = DateTime.Now;

                    _context.Add(standard);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "استاندارد جدید با موفقیت ایجاد شد";
                    return RedirectToAction(nameof(QAStandardList));
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "خطا در ایجاد استاندارد: " + ex.Message;
                }
            }

            return View(standard);
        }

        // GET: QAStandard/EditQAStandard/5
        public async Task<IActionResult> EditQAStandard(Guid id)
        {
            var standard = await _context.QaStandards.FindAsync(id);
            if (standard == null)
            {
                return NotFound();
            }

            return View(standard);
        }

        // POST: QAStandard/EditQAStandard/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditQAStandard(Guid id, QaStandard standard)
        {
            if (id != standard.StandardId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (await _context.QaStandards.AnyAsync(q =>
                        q.StandardId != id &&
                        q.StandardCode == standard.StandardCode))
                    {
                        TempData["ErrorMessage"] = "استاندارد با این کد قبلاً ثبت شده است";
                        return View(standard);
                    }

                    var existingStandard = await _context.QaStandards.FindAsync(id);
                    if (existingStandard == null)
                    {
                        return NotFound();
                    }

                    // Keep original creation data
                    standard.CreatedDate = existingStandard.CreatedDate;
                    standard.CreatedBy = existingStandard.CreatedBy;
                    standard.LastModifiedDate = DateTime.Now;

                    _context.Entry(existingStandard).CurrentValues.SetValues(standard);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "اطلاعات استاندارد با موفقیت به‌روزرسانی شد";
                    return RedirectToAction(nameof(QAStandardList));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!QAStandardExists(standard.StandardId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            return View(standard);
        }

        // POST: QAStandard/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var standard = await _context.QaStandards.FindAsync(id);
            if (standard == null)
            {
                TempData["ErrorMessage"] = "استاندارد مورد نظر یافت نشد";
                return RedirectToAction(nameof(QAStandardList));
            }

            try
            {
                _context.QaStandards.Remove(standard);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "استاندارد با موفقیت حذف شد";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "خطا در حذف استاندارد: " + ex.Message;
            }

            return RedirectToAction(nameof(QAStandardList));
        }

        private bool QAStandardExists(Guid id)
        {
            return _context.QaStandards.Any(e => e.StandardId == id);
        }
    }
}