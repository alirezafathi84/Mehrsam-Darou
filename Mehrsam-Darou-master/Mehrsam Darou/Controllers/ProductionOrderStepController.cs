using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mehrsam_Darou.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using static Mehrsam_Darou.Helper.Helper;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace Mehrsam_Darou.Controllers
{
    public class ProductionOrderStepController : BaseController
    {
        private readonly DarouAppContext _context;

        public ProductionOrderStepController(DarouAppContext context) : base(context)
        {
            _context = context;
        }

        // GET: ProductionOrderStep/ProductionOrderStepList
        public async Task<IActionResult> ProductionOrderStepList(Guid orderId)
        {
            var order = await _context.ProductionOrders
                .Include(o => o.Medicine)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
            {
                return NotFound();
            }

            ViewBag.OrderInfo = order;

            var steps = await _context.ProductionOrderSteps
                .Include(s => s.Step)
                .Where(s => s.OrderId == orderId)
                .OrderBy(s => s.Step.Sequence)
                .ToListAsync();

            return View(steps);
        }

        // GET: ProductionOrderStep/AddProductionOrderStep
        public async Task<IActionResult> AddProductionOrderStep(Guid orderId)
        {
            var order = await _context.ProductionOrders
                .Include(o => o.Medicine)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
            {
                return NotFound();
            }

            ViewBag.OrderInfo = order;

            await PopulateProductionOrderStepDropdowns(orderId);
            return View(new ProductionOrderStep
            {
                OrderId = orderId,
                Status = "در انتظار" // Changed to Persian to match database constraint
            });
        }

        // POST: ProductionOrderStep/AddProductionOrderStep
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddProductionOrderStep(Guid orderId, ProductionOrderStep productionOrderStep)
        {
            // Remove ModelState errors for navigation properties
            ModelState.Remove("Order");
            ModelState.Remove("Step");

            if (ModelState.IsValid)
            {
                try
                {
                    if (await _context.ProductionOrderSteps.AnyAsync(s =>
                        s.OrderId == orderId &&
                        s.StepId == productionOrderStep.StepId))
                    {
                        TempData["ErrorMessage"] = "این مرحله قبلاً برای این سفارش اضافه شده است";
                        await PopulateProductionOrderStepDropdowns(orderId);
                        return View(productionOrderStep);
                    }

                    // Clear navigation properties to avoid conflicts
                    productionOrderStep.Order = null;
                    productionOrderStep.Step = null;

                    // Set OrderStepId if it's empty (for new records)
                    if (productionOrderStep.OrderStepId == Guid.Empty)
                    {
                        productionOrderStep.OrderStepId = Guid.NewGuid();
                    }

                    _context.Add(productionOrderStep);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "مرحله تولید با موفقیت به سفارش اضافه شد";
                    return RedirectToAction(nameof(ProductionOrderStepList), new { orderId });
                }
                catch (Exception ex)
                {
                    // Get detailed error information
                    var innerException = ex.InnerException?.Message ?? ex.Message;
                    var detailedError = $"خطا در اضافه کردن مرحله تولید: {ex.Message}";

                    if (ex.InnerException != null)
                    {
                        detailedError += $" - جزئیات: {ex.InnerException.Message}";
                    }

                    TempData["ErrorMessage"] = detailedError;
                }
            }
            else
            {
                // Add ModelState errors to TempData for debugging
                var errors = string.Join("; ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));
                TempData["ErrorMessage"] = $"خطاهای اعتبارسنجی: {errors}";
            }

            await PopulateProductionOrderStepDropdowns(orderId);
            return View(productionOrderStep);
        }

        // GET: ProductionOrderStep/EditProductionOrderStep/5
        public async Task<IActionResult> EditProductionOrderStep(Guid id)
        {
            var productionOrderStep = await _context.ProductionOrderSteps
                .Include(s => s.Order)
                .Include(s => s.Step)
                .FirstOrDefaultAsync(s => s.OrderStepId == id);

            if (productionOrderStep == null)
            {
                return NotFound();
            }

            ViewBag.OrderInfo = productionOrderStep.Order;
            await PopulateProductionOrderStepDropdowns(productionOrderStep.OrderId, productionOrderStep.StepId);
            return View(productionOrderStep);
        }

        // POST: ProductionOrderStep/EditProductionOrderStep/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProductionOrderStep(Guid id, ProductionOrderStep productionOrderStep)
        {
            if (id != productionOrderStep.OrderStepId)
            {
                return NotFound();
            }

            // Remove ModelState errors for navigation properties
            ModelState.Remove("Order");
            ModelState.Remove("Step");

            if (ModelState.IsValid)
            {
                try
                {
                    // Clear navigation properties to avoid conflicts
                    productionOrderStep.Order = null;
                    productionOrderStep.Step = null;

                    _context.Update(productionOrderStep);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "اطلاعات مرحله تولید با موفقیت به‌روزرسانی شد";
                    return RedirectToAction(nameof(ProductionOrderStepList), new { orderId = productionOrderStep.OrderId });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductionOrderStepExists(productionOrderStep.OrderStepId))
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
                    // Get detailed error information
                    var innerException = ex.InnerException?.Message ?? ex.Message;
                    var detailedError = $"خطا در به‌روزرسانی مرحله تولید: {ex.Message}";

                    if (ex.InnerException != null)
                    {
                        detailedError += $" - جزئیات: {ex.InnerException.Message}";
                    }

                    TempData["ErrorMessage"] = detailedError;
                }
            }
            else
            {
                // Add ModelState errors to TempData for debugging
                var errors = string.Join("; ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));
                TempData["ErrorMessage"] = $"خطاهای اعتبارسنجی: {errors}";
            }

            await PopulateProductionOrderStepDropdowns(productionOrderStep.OrderId, productionOrderStep.StepId);
            return View(productionOrderStep);
        }

        // POST: ProductionOrderStep/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var productionOrderStep = await _context.ProductionOrderSteps
                .Include(s => s.Order)
                .FirstOrDefaultAsync(s => s.OrderStepId == id);

            if (productionOrderStep == null)
            {
                TempData["ErrorMessage"] = "مرحله تولید مورد نظر یافت نشد";
                return RedirectToAction("ProductionOrderList", "ProductionOrder");
            }

            var orderId = productionOrderStep.OrderId;

            try
            {
                _context.ProductionOrderSteps.Remove(productionOrderStep);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "مرحله تولید با موفقیت حذف شد";
            }
            catch (Exception ex)
            {
                var innerException = ex.InnerException?.Message ?? ex.Message;
                var detailedError = $"خطا در حذف مرحله تولید: {ex.Message}";

                if (ex.InnerException != null)
                {
                    detailedError += $" - جزئیات: {ex.InnerException.Message}";
                }

                TempData["ErrorMessage"] = detailedError;
            }

            return RedirectToAction(nameof(ProductionOrderStepList), new { orderId });
        }

        private bool ProductionOrderStepExists(Guid id)
        {
            return _context.ProductionOrderSteps.Any(e => e.OrderStepId == id);
        }

        private async Task PopulateProductionOrderStepDropdowns(Guid orderId, Guid? currentStepId = null)
        {
            try
            {
                // Get order
                var order = await _context.ProductionOrders.FindAsync(orderId);

                // Get all active steps
                var allSteps = await _context.ProductionSteps
                    .Where(s => s.IsActive == true)
                    .OrderBy(s => s.Sequence)
                    .ToListAsync();

                // Get steps already assigned to this order
                var assignedSteps = await _context.ProductionOrderSteps
                    .Where(s => s.OrderId == orderId && s.StepId != currentStepId)
                    .Select(s => s.StepId)
                    .ToListAsync();

                // Filter out already assigned steps
                var availableSteps = allSteps
                    .Where(s => !assignedSteps.Contains(s.StepId))
                    .ToList();

                ViewBag.Steps = availableSteps != null && availableSteps.Any()
                    ? new SelectList(availableSteps, "StepId", "StepName")
                    : new SelectList(new List<object>(), "Value", "Text");
            }
            catch (Exception)
            {
                // Fallback to empty list if database queries fail
                ViewBag.Steps = new SelectList(new List<object>(), "Value", "Text");
            }

            // Statuses - Fixed to match database CHECK constraint (Persian values)
            var statuses = new List<SelectListItem>
            {
                new SelectListItem { Value = "در انتظار", Text = "در انتظار" },
                new SelectListItem { Value = "در حال اجرا", Text = "در حال اجرا" },
                new SelectListItem { Value = "تکمیل شده", Text = "تکمیل شده" },
                new SelectListItem { Value = "ناموفق", Text = "ناموفق" }
            };

            ViewBag.Statuses = new SelectList(statuses, "Value", "Text");
        }
    }
}