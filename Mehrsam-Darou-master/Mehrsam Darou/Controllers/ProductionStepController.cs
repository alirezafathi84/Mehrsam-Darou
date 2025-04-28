using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mehrsam_Darou.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using static Mehrsam_Darou.Helper.Helper;

namespace Mehrsam_Darou.Controllers
{
    public class ProductionStepController : BaseController
    {
        private readonly DarouAppContext _context;

        public ProductionStepController(DarouAppContext context) : base(context)
        {
            _context = context;
        }

        // GET: ProductionStep/ProductionStepList
        public async Task<IActionResult> ProductionStepList(int? page, string searchKey)
        {
            var setting = await ReadSettingAsync(_context);
            int pageSize = Convert.ToInt32(setting.NumberPerPage ?? 10);
            int pageNumber = page ?? 1;

            IQueryable<ProductionStep> query = _context.ProductionSteps;

            if (!string.IsNullOrWhiteSpace(searchKey))
            {
                query = query.Where(s => s.StepName.Contains(searchKey))
                            .OrderBy(s => s.Sequence);
            }

            int total = await query.CountAsync();
            var items = await query
                .OrderBy(s => s.Sequence)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var paginatedList = new PaginatedList<ProductionStep>(items, total, pageNumber, pageSize);

            return View(paginatedList);
        }

        // GET: ProductionStep/AddProductionStep
        public IActionResult AddProductionStep()
        {
            // Get next sequence number
            int nextSequence = _context.ProductionSteps.Any() ?
                _context.ProductionSteps.Max(s => s.Sequence) + 1 : 1;

            return View(new ProductionStep
            {
                Sequence = nextSequence,
                IsActive = true
            });
        }

        // POST: ProductionStep/AddProductionStep
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddProductionStep(ProductionStep productionStep)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    if (await _context.ProductionSteps.AnyAsync(s => s.StepName == productionStep.StepName))
                    {
                        TempData["ErrorMessage"] = "مرحله با این نام قبلاً ثبت شده است";
                        return View(productionStep);
                    }

                    _context.Add(productionStep);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "مرحله تولید جدید با موفقیت ایجاد شد";
                    return RedirectToAction(nameof(ProductionStepList));
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "خطا در ایجاد مرحله تولید: " + ex.Message;
                }
            }
            return View(productionStep);
        }

        // GET: ProductionStep/EditProductionStep/5
        public async Task<IActionResult> EditProductionStep(Guid id)
        {
            var productionStep = await _context.ProductionSteps.FindAsync(id);
            if (productionStep == null)
            {
                return NotFound();
            }
            return View(productionStep);
        }

        // POST: ProductionStep/EditProductionStep/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProductionStep(Guid id, ProductionStep productionStep)
        {
            if (id != productionStep.StepId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (await _context.ProductionSteps.AnyAsync(s =>
                        s.StepId != id &&
                        s.StepName == productionStep.StepName))
                    {
                        TempData["ErrorMessage"] = "مرحله با این نام قبلاً ثبت شده است";
                        return View(productionStep);
                    }

                    _context.Update(productionStep);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "اطلاعات مرحله تولید با موفقیت به‌روزرسانی شد";
                    return RedirectToAction(nameof(ProductionStepList));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductionStepExists(productionStep.StepId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            return View(productionStep);
        }

        // POST: ProductionStep/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var productionStep = await _context.ProductionSteps.FindAsync(id);
            if (productionStep == null)
            {
                TempData["ErrorMessage"] = "مرحله تولید مورد نظر یافت نشد";
                return RedirectToAction(nameof(ProductionStepList));
            }

            // Check if step is used in any orders
            bool isUsed = await _context.ProductionOrderSteps.AnyAsync(o => o.StepId == id);
            if (isUsed)
            {
                TempData["ErrorMessage"] = "این مرحله در سفارشات تولید استفاده شده و قابل حذف نیست";
                return RedirectToAction(nameof(ProductionStepList));
            }

            try
            {
                _context.ProductionSteps.Remove(productionStep);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "مرحله تولید با موفقیت حذف شد";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "خطا در حذف مرحله تولید: " + ex.Message;
            }

            return RedirectToAction(nameof(ProductionStepList));
        }

        private bool ProductionStepExists(Guid id)
        {
            return _context.ProductionSteps.Any(e => e.StepId == id);
        }
    }
}