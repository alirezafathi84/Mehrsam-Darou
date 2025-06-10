using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mehrsam_Darou.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using static Mehrsam_Darou.Helper.Helper;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Mehrsam_Darou.Controllers
{
    public class CertificationController : BaseController
    {
        private readonly DarouAppContext _context;

        public CertificationController(DarouAppContext context) : base(context)
        {
            _context = context;
        }

        // GET: Certification/Index
        public async Task<IActionResult> Index(int? page, string searchKey, string statusFilter, string typeFilter)
        {
            return await CertificationList(page, searchKey, statusFilter, typeFilter);
        }

        // GET: Certification/CertificationList
        public async Task<IActionResult> CertificationList(int? page, string searchKey, string statusFilter, string typeFilter)
        {
            var setting = await ReadSettingAsync(_context);
            int pageSize = Convert.ToInt32(setting.NumberPerPage ?? 10);
            int pageNumber = page ?? 1;

            IQueryable<Certification> query = _context.Certifications
                .Include(c => c.RelatedMedicine);

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(searchKey))
            {
                query = query.Where(c => c.CertificationName.Contains(searchKey) ||
                                     c.CertificationCode.Contains(searchKey) ||
                                     c.IssuingAuthority.Contains(searchKey) ||
                                     c.ResponsiblePerson.Contains(searchKey) ||
                                     c.CertificateNumber.Contains(searchKey));
            }

            // Apply status filter
            if (!string.IsNullOrWhiteSpace(statusFilter))
            {
                query = query.Where(c => c.CertificationStatus == statusFilter);
            }

            // Apply type filter
            if (!string.IsNullOrWhiteSpace(typeFilter))
            {
                query = query.Where(c => c.CertificationType == typeFilter);
            }

            int total = await query.CountAsync();
            var items = await query
                .OrderBy(c => c.ExpiryDate ?? DateOnly.MaxValue)
                .ThenBy(c => c.CertificationName)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var paginatedList = new PaginatedList<Certification>(items, total, pageNumber, pageSize);

            // Pass filter values to view
            ViewBag.StatusFilter = statusFilter;
            ViewBag.TypeFilter = typeFilter;
            ViewBag.SearchKey = searchKey;

            // Get filter options
            ViewBag.StatusOptions = await _context.Certifications
                .Where(c => !string.IsNullOrEmpty(c.CertificationStatus))
                .Select(c => c.CertificationStatus)
                .Distinct()
                .OrderBy(s => s)
                .ToListAsync();

            ViewBag.TypeOptions = await _context.Certifications
                .Where(c => !string.IsNullOrEmpty(c.CertificationType))
                .Select(c => c.CertificationType)
                .Distinct()
                .OrderBy(t => t)
                .ToListAsync();

            return View(paginatedList);
        }

        // GET: Certification/AddCertification
        public async Task<IActionResult> AddCertification()
        {
            await PopulateDropdowns();
            return View(new Certification
            {
                IsActive = true,
                CreatedDate = DateTime.Now,
                PriorityLevel = 3,
                Currency = "IRR",
                RenewalReminderDays = 90,
                CorrectiveActionsRequired = false
            });
        }

        // POST: Certification/AddCertification
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCertification(Certification certification)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    if (await _context.Certifications.AnyAsync(c => c.CertificationCode == certification.CertificationCode))
                    {
                        TempData["ErrorMessage"] = "گواهینامه با این کد قبلاً ثبت شده است";
                        await PopulateDropdowns();
                        return View(certification);
                    }

                    // Validate dates
                    if (certification.IssueDate.HasValue && certification.ExpiryDate.HasValue &&
                        certification.ExpiryDate.Value <= certification.IssueDate.Value)
                    {
                        TempData["ErrorMessage"] = "تاریخ انقضا باید بعد از تاریخ صدور باشد";
                        await PopulateDropdowns();
                        return View(certification);
                    }

                    certification.CertificationId = Guid.NewGuid();
                    certification.CreatedDate = DateTime.Now;

                    // Calculate next audit date if audit frequency is provided
                    if (certification.AuditFrequencyMonths.HasValue && certification.LastAuditDate.HasValue)
                    {
                        certification.NextAuditDate = DateOnly.FromDateTime(certification.LastAuditDate.Value.ToDateTime(TimeOnly.MinValue).AddMonths(certification.AuditFrequencyMonths.Value));
                    }

                    _context.Add(certification);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "گواهینامه جدید با موفقیت ایجاد شد";
                    return RedirectToAction(nameof(CertificationList));
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "خطا در ایجاد گواهینامه: " + ex.Message;
                }
            }

            await PopulateDropdowns();
            return View(certification);
        }

        // GET: Certification/EditCertification/5
        public async Task<IActionResult> EditCertification(Guid id)
        {
            var certification = await _context.Certifications
                .Include(c => c.RelatedMedicine)
                .FirstOrDefaultAsync(c => c.CertificationId == id);

            if (certification == null)
            {
                return NotFound();
            }

            await PopulateDropdowns();
            return View(certification);
        }

        // POST: Certification/EditCertification/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCertification(Guid id, Certification certification)
        {
            if (id != certification.CertificationId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (await _context.Certifications.AnyAsync(c =>
                        c.CertificationId != id &&
                        c.CertificationCode == certification.CertificationCode))
                    {
                        TempData["ErrorMessage"] = "گواهینامه با این کد قبلاً ثبت شده است";
                        await PopulateDropdowns();
                        return View(certification);
                    }

                    // Validate dates
                    if (certification.IssueDate.HasValue && certification.ExpiryDate.HasValue &&
                        certification.ExpiryDate.Value <= certification.IssueDate.Value)
                    {
                        TempData["ErrorMessage"] = "تاریخ انقضا باید بعد از تاریخ صدور باشد";
                        await PopulateDropdowns();
                        return View(certification);
                    }

                    var existingCertification = await _context.Certifications.FindAsync(id);
                    if (existingCertification == null)
                    {
                        return NotFound();
                    }

                    // Keep the original creation data
                    certification.CreatedDate = existingCertification.CreatedDate;
                    certification.CreatedBy = existingCertification.CreatedBy;
                    certification.LastModifiedDate = DateTime.Now;

                    // Calculate next audit date if audit frequency is provided
                    if (certification.AuditFrequencyMonths.HasValue && certification.LastAuditDate.HasValue)
                    {
                        certification.NextAuditDate = DateOnly.FromDateTime(certification.LastAuditDate.Value.ToDateTime(TimeOnly.MinValue).AddMonths(certification.AuditFrequencyMonths.Value));
                    }

                    _context.Entry(existingCertification).CurrentValues.SetValues(certification);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "اطلاعات گواهینامه با موفقیت به‌روزرسانی شد";
                    return RedirectToAction(nameof(CertificationList));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CertificationExists(certification.CertificationId))
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
            return View(certification);
        }

        // POST: Certification/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var certification = await _context.Certifications.FindAsync(id);
            if (certification == null)
            {
                TempData["ErrorMessage"] = "گواهینامه مورد نظر یافت نشد";
                return RedirectToAction(nameof(CertificationList));
            }

            try
            {
                _context.Certifications.Remove(certification);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "گواهینامه با موفقیت حذف شد";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "خطا در حذف گواهینامه: " + ex.Message;
            }

            return RedirectToAction(nameof(CertificationList));
        }

        // GET: Certification/Details/5
        public async Task<IActionResult> Details(Guid id)
        {
            var certification = await _context.Certifications
                .Include(c => c.RelatedMedicine)
                .FirstOrDefaultAsync(c => c.CertificationId == id);

            if (certification == null)
            {
                return NotFound();
            }

            return View(certification);
        }

        // GET: Certification/ExpiringCertifications
        public async Task<IActionResult> ExpiringCertifications(int? days)
        {
            int reminderDays = days ?? 90;
            var today = DateOnly.FromDateTime(DateTime.Now);
            var futureDate = DateOnly.FromDateTime(DateTime.Now.AddDays(reminderDays));

            var expiringCertifications = await _context.Certifications
                .Include(c => c.RelatedMedicine)
                .Where(c => c.IsActive == true &&
                           c.ExpiryDate.HasValue &&
                           c.ExpiryDate.Value >= today &&
                           c.ExpiryDate.Value <= futureDate)
                .OrderBy(c => c.ExpiryDate)
                .ToListAsync();

            ViewBag.ReminderDays = reminderDays;
            return View(expiringCertifications);
        }

        private bool CertificationExists(Guid id)
        {
            return _context.Certifications.Any(e => e.CertificationId == id);
        }

        private async Task PopulateDropdowns()
        {
            ViewBag.Medicines = new SelectList(
                await _context.Medicines
                    .Where(m => m.IsActive == true)
                    .OrderBy(m => m.BrandName)
                    .Select(m => new { m.MedicineId, m.BrandName })
                    .ToListAsync(),
                "MedicineId",
                "BrandName"
            );
        }
    }
}