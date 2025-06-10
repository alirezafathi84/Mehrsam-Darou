using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mehrsam_Darou.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using static Mehrsam_Darou.Helper.Helper;

namespace Mehrsam_Darou.Controllers
{
    public class QAAuditController : BaseController
    {
        private readonly DarouAppContext _context;

        public QAAuditController(DarouAppContext context) : base(context)
        {
            _context = context;
        }

        // GET: QAAudit/QAAuditList
        public async Task<IActionResult> QAAuditList(int? page, string searchKey)
        {
            var setting = await ReadSettingAsync(_context);
            int pageSize = Convert.ToInt32(setting.NumberPerPage ?? 10);
            int pageNumber = page ?? 1;

            IQueryable<QaAudit> query = _context.QaAudits
                .Include(q => q.RelatedProduct)
                .Include(q => q.CreatedByNavigation);

            if (!string.IsNullOrWhiteSpace(searchKey))
            {
                query = query.Where(q => q.AuditCode.Contains(searchKey) ||
                                     q.AuditTitle.Contains(searchKey) ||
                                     q.AuditedDepartment.Contains(searchKey) ||
                                     q.LeadAuditor.Contains(searchKey))
                            .OrderBy(q => q.AuditTitle);
            }

            int total = await query.CountAsync();
            var items = await query
                .OrderByDescending(q => q.CreatedDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var paginatedList = new PaginatedList<QaAudit>(items, total, pageNumber, pageSize);

            return View(paginatedList);
        }

        // GET: QAAudit/AddQAAudit
        public async Task<IActionResult> AddQAAudit()
        {
            ViewBag.Medicines = await _context.Medicines
                .Where(m => m.IsActive == true)
                .Select(m => new { m.MedicineId, m.BrandName })
                .ToListAsync();

            return View(new QaAudit
            {
                IsActive = true,
                CreatedDate = DateTime.Now,
                AuditPriority = 3,
                ObservationsCount = 0,
                MinorNonconformities = 0,
                MajorNonconformities = 0,
                CriticalNonconformities = 0,
                OpportunitiesForImprovement = 0,
                CorrectiveActionsRequired = false,
                PreventiveActionsRequired = false,
                FollowUpRequired = false,
                ManagementReviewRequired = false,
                Currency = "IRR"
            });
        }

        // POST: QAAudit/AddQAAudit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddQAAudit(QaAudit audit)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    if (await _context.QaAudits.AnyAsync(q => q.AuditCode == audit.AuditCode))
                    {
                        TempData["ErrorMessage"] = "ممیزی با این کد قبلاً ثبت شده است";
                        await LoadViewBagData();
                        return View(audit);
                    }

                    audit.AuditId = Guid.NewGuid();
                    audit.CreatedDate = DateTime.Now;

                    _context.Add(audit);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "ممیزی جدید با موفقیت ایجاد شد";
                    return RedirectToAction(nameof(QAAuditList));
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "خطا در ایجاد ممیزی: " + ex.Message;
                }
            }

            await LoadViewBagData();
            return View(audit);
        }

        // GET: QAAudit/EditQAAudit/5
        public async Task<IActionResult> EditQAAudit(Guid id)
        {
            var audit = await _context.QaAudits.FindAsync(id);
            if (audit == null)
            {
                return NotFound();
            }

            await LoadViewBagData();
            return View(audit);
        }

        // POST: QAAudit/EditQAAudit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditQAAudit(Guid id, QaAudit audit)
        {
            if (id != audit.AuditId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (await _context.QaAudits.AnyAsync(q =>
                        q.AuditId != id &&
                        q.AuditCode == audit.AuditCode))
                    {
                        TempData["ErrorMessage"] = "ممیزی با این کد قبلاً ثبت شده است";
                        await LoadViewBagData();
                        return View(audit);
                    }

                    var existingAudit = await _context.QaAudits.FindAsync(id);
                    if (existingAudit == null)
                    {
                        return NotFound();
                    }

                    // Keep original creation data
                    audit.CreatedDate = existingAudit.CreatedDate;
                    audit.CreatedBy = existingAudit.CreatedBy;
                    audit.LastModifiedDate = DateTime.Now;

                    _context.Entry(existingAudit).CurrentValues.SetValues(audit);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "اطلاعات ممیزی با موفقیت به‌روزرسانی شد";
                    return RedirectToAction(nameof(QAAuditList));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!QAAuditExists(audit.AuditId))
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
            return View(audit);
        }

        // POST: QAAudit/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var audit = await _context.QaAudits.FindAsync(id);
            if (audit == null)
            {
                TempData["ErrorMessage"] = "ممیزی مورد نظر یافت نشد";
                return RedirectToAction(nameof(QAAuditList));
            }

            try
            {
                _context.QaAudits.Remove(audit);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "ممیزی با موفقیت حذف شد";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "خطا در حذف ممیزی: " + ex.Message;
            }

            return RedirectToAction(nameof(QAAuditList));
        }

        private bool QAAuditExists(Guid id)
        {
            return _context.QaAudits.Any(e => e.AuditId == id);
        }

        private async Task LoadViewBagData()
        {
            ViewBag.Medicines = await _context.Medicines
                .Where(m => m.IsActive == true)
                .Select(m => new { m.MedicineId, m.BrandName })
                .ToListAsync();
        }
    }
}