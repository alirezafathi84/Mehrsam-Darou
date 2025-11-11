using Mehrsam_Darou.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Mehrsam_Darou.Helper.Helper;

namespace Mehrsam_Darou.Controllers
{
    public class CostCenterController : BaseController
    {
        private readonly DarouAppContext _context;

        public CostCenterController(DarouAppContext context) : base(context)
        {
            _context = context;
        }

        // GET: CostCenter List
        public async Task<IActionResult> CostCenterList(int? page, string SearchKey)
        {
            // Set common view data and get the page size
            var setting = await ReadSettingAsync(_context);
            int pageSize = Convert.ToInt32(setting.NumberPerPage ?? 10);
            int pageNumber = page ?? 1;

            // Base query for fetching cost centers
            IQueryable<CostCenter> query = _context.CostCenters
                .Include(c => c.ParentCostCenter);

            // Apply search filter if SearchKey is provided
            if (!string.IsNullOrWhiteSpace(SearchKey))
            {
                query = query.Where(c => 
                    c.CostCenterCode.Contains(SearchKey) || 
                    c.CostCenterName.Contains(SearchKey) ||
                    (c.Description != null && c.Description.Contains(SearchKey))
                );
            }

            // Get total count after filtering
            int total = await query.CountAsync();

            // Fetch paginated results
            var items = await query
                .OrderBy(c => c.CostCenterCode)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Create the paginated list
            var paginatedList = new PaginatedList<CostCenter>(items, total, pageNumber, pageSize);

            // Pass all cost centers for parent dropdown
            ViewBag.CostCenters = await _context.CostCenters
                .Where(c => c.IsActive == true)
                .OrderBy(c => c.CostCenterCode)
                .ToListAsync();

            return View(paginatedList);
        }

        // GET: Add Cost Center
        [HttpGet]
        public async Task<IActionResult> AddCostCenter()
        {
            // Get all active cost centers for parent dropdown
            ViewBag.ParentCostCenters = await _context.CostCenters
                .Where(c => c.IsActive == true)
                .OrderBy(c => c.CostCenterCode)
                .ToListAsync();

            return View();
        }

        // POST: Add Cost Center
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCostCenter(CostCenter costCenter)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Check if cost center code already exists
                    var existingCode = await _context.CostCenters
                        .AnyAsync(c => c.CostCenterCode == costCenter.CostCenterCode);

                    if (existingCode)
                    {
                        TempData["ErrorMessage"] = "کد مرکز هزینه تکراری است";
                        ViewBag.ParentCostCenters = await _context.CostCenters
                            .Where(c => c.IsActive == true)
                            .OrderBy(c => c.CostCenterCode)
                            .ToListAsync();
                        return View(costCenter);
                    }

                    costCenter.CostCenterId = Guid.NewGuid();
                    costCenter.CreatedDate = DateTime.Now;
                    costCenter.IsActive = costCenter.IsActive ?? true;

                    _context.CostCenters.Add(costCenter);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "مرکز هزینه با موفقیت ایجاد شد";
                    return RedirectToAction(nameof(CostCenterList));
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "خطا در ایجاد مرکز هزینه: " + ex.Message;
                }
            }
            else
            {
                TempData["ErrorMessage"] = "لطفاً اطلاعات را صحیح وارد نمایید";
            }

            ViewBag.ParentCostCenters = await _context.CostCenters
                .Where(c => c.IsActive == true)
                .OrderBy(c => c.CostCenterCode)
                .ToListAsync();

            return View(costCenter);
        }

        // GET: Edit Cost Center
        [HttpGet]
        public async Task<IActionResult> EditCostCenter(Guid id)
        {
            var costCenter = await _context.CostCenters
                .Include(c => c.ParentCostCenter)
                .FirstOrDefaultAsync(c => c.CostCenterId == id);

            if (costCenter == null)
            {
                TempData["ErrorMessage"] = "مرکز هزینه مورد نظر یافت نشد";
                return RedirectToAction(nameof(CostCenterList));
            }

            // Get all cost centers except current one for parent dropdown
            ViewBag.ParentCostCenters = await _context.CostCenters
                .Where(c => c.IsActive == true && c.CostCenterId != id)
                .OrderBy(c => c.CostCenterCode)
                .ToListAsync();

            return View(costCenter);
        }

        // POST: Edit Cost Center
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCostCenter(CostCenter costCenter)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "اطلاعات وارد شده معتبر نیست";
                ViewBag.ParentCostCenters = await _context.CostCenters
                    .Where(c => c.IsActive == true && c.CostCenterId != costCenter.CostCenterId)
                    .OrderBy(c => c.CostCenterCode)
                    .ToListAsync();
                return View(costCenter);
            }

            var existingCostCenter = await _context.CostCenters
                .FindAsync(costCenter.CostCenterId);

            if (existingCostCenter == null)
            {
                TempData["ErrorMessage"] = "مرکز هزینه مورد نظر یافت نشد";
                return RedirectToAction(nameof(CostCenterList));
            }

            try
            {
                // Check if cost center code already exists (excluding current record)
                var duplicateCode = await _context.CostCenters
                    .AnyAsync(c => c.CostCenterCode == costCenter.CostCenterCode 
                                && c.CostCenterId != costCenter.CostCenterId);

                if (duplicateCode)
                {
                    TempData["ErrorMessage"] = "کد مرکز هزینه تکراری است";
                    ViewBag.ParentCostCenters = await _context.CostCenters
                        .Where(c => c.IsActive == true && c.CostCenterId != costCenter.CostCenterId)
                        .OrderBy(c => c.CostCenterCode)
                        .ToListAsync();
                    return View(costCenter);
                }

                // Validate parent cost center (prevent circular reference)
                if (costCenter.ParentCostCenterId.HasValue)
                {
                    var isCircular = await IsCircularReference(
                        costCenter.CostCenterId, 
                        costCenter.ParentCostCenterId.Value
                    );

                    if (isCircular)
                    {
                        TempData["ErrorMessage"] = "نمی‌توان مرکز هزینه را به عنوان والد خود یا فرزندان خود تعیین کرد";
                        ViewBag.ParentCostCenters = await _context.CostCenters
                            .Where(c => c.IsActive == true && c.CostCenterId != costCenter.CostCenterId)
                            .OrderBy(c => c.CostCenterCode)
                            .ToListAsync();
                        return View(costCenter);
                    }
                }

                // Update properties
                existingCostCenter.CostCenterCode = costCenter.CostCenterCode;
                existingCostCenter.CostCenterName = costCenter.CostCenterName;
                existingCostCenter.Description = costCenter.Description;
                existingCostCenter.ParentCostCenterId = costCenter.ParentCostCenterId;
                existingCostCenter.IsActive = costCenter.IsActive;

                _context.CostCenters.Update(existingCostCenter);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "اطلاعات مرکز هزینه با موفقیت به‌روزرسانی شد";
                return RedirectToAction(nameof(CostCenterList));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "خطایی در به‌روزرسانی اطلاعات رخ داد: " + ex.Message;
                ViewBag.ParentCostCenters = await _context.CostCenters
                    .Where(c => c.IsActive == true && c.CostCenterId != costCenter.CostCenterId)
                    .OrderBy(c => c.CostCenterCode)
                    .ToListAsync();
                return View(costCenter);
            }
        }

        // POST: Delete Cost Center
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCostCenter(Guid id)
        {
            var costCenter = await _context.CostCenters
                .Include(c => c.InverseParentCostCenter)
                .Include(c => c.MaterialRequests)
                .FirstOrDefaultAsync(c => c.CostCenterId == id);

            if (costCenter == null)
            {
                TempData["ErrorMessage"] = "مرکز هزینه مورد نظر یافت نشد";
                return RedirectToAction(nameof(CostCenterList));
            }

            // Check if cost center has children
            if (costCenter.InverseParentCostCenter.Any())
            {
                TempData["ErrorMessage"] = "نمی‌توان مرکز هزینه با زیرمجموعه را حذف کرد";
                return RedirectToAction(nameof(CostCenterList));
            }

            // Check if cost center has material requests
            if (costCenter.MaterialRequests.Any())
            {
                TempData["ErrorMessage"] = "نمی‌توان مرکز هزینه با درخواست مواد مرتبط را حذف کرد";
                return RedirectToAction(nameof(CostCenterList));
            }

            try
            {
                _context.CostCenters.Remove(costCenter);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "مرکز هزینه با موفقیت حذف شد";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "خطا در حذف مرکز هزینه: " + ex.Message;
            }

            return RedirectToAction(nameof(CostCenterList));
        }

        // Helper method to check circular reference
        private async Task<bool> IsCircularReference(Guid costCenterId, Guid parentId)
        {
            var currentParent = await _context.CostCenters
                .FirstOrDefaultAsync(c => c.CostCenterId == parentId);

            while (currentParent != null)
            {
                if (currentParent.CostCenterId == costCenterId)
                {
                    return true; // Circular reference detected
                }

                if (currentParent.ParentCostCenterId.HasValue)
                {
                    currentParent = await _context.CostCenters
                        .FirstOrDefaultAsync(c => c.CostCenterId == currentParent.ParentCostCenterId.Value);
                }
                else
                {
                    break;
                }
            }

            return false;
        }
    }
}
