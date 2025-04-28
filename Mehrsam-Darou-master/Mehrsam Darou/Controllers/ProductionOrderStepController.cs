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
                Status = "Pending"
            });
        }

        // POST: ProductionOrderStep/AddProductionOrderStep
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddProductionOrderStep(Guid orderId, ProductionOrderStep productionOrderStep)
        {
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

                    _context.Add(productionOrderStep);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "مرحله تولید با موفقیت به سفارش اضافه شد";
                    return RedirectToAction(nameof(ProductionOrderStepList), new { orderId });
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "خطا در اضافه کردن مرحله تولید: " + ex.Message;
                }
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

            if (ModelState.IsValid)
            {
                try
                {
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
                return RedirectToAction(nameof(ProductionOrderStepList), new { orderId = productionOrderStep.OrderId });
            }

            try
            {
                _context.ProductionOrderSteps.Remove(productionOrderStep);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "مرحله تولید با موفقیت حذف شد";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "خطا در حذف مرحله تولید: " + ex.Message;
            }

            return RedirectToAction(nameof(ProductionOrderStepList), new { orderId = productionOrderStep.OrderId });
        }

        private bool ProductionOrderStepExists(Guid id)
        {
            return _context.ProductionOrderSteps.Any(e => e.OrderStepId == id);
        }

        private async Task PopulateProductionOrderStepDropdowns(Guid orderId, Guid? currentStepId = null)
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

            ViewBag.Steps = new SelectList(availableSteps, "StepId", "StepName");

            // Statuses
            var statuses = new List<SelectListItem>
            {
                new SelectListItem { Value = "Pending", Text = "در انتظار" },
                new SelectListItem { Value = "In Progress", Text = "در حال انجام" },
                new SelectListItem { Value = "Completed", Text = "تکمیل شده" },
                new SelectListItem { Value = "Failed", Text = "ناموفق" }
            };

            ViewBag.Statuses = new SelectList(statuses, "Value", "Text");
        }
    }
}