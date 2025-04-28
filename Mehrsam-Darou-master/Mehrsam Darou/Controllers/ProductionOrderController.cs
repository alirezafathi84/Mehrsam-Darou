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
    public class ProductionOrderController : BaseController
    {
        private readonly DarouAppContext _context;

        public ProductionOrderController(DarouAppContext context) : base(context)
        {
            _context = context;
        }

        // GET: ProductionOrder/ProductionOrderList
        public async Task<IActionResult> ProductionOrderList(int? page, string searchKey)
        {
            var setting = await ReadSettingAsync(_context);
            int pageSize = Convert.ToInt32(setting.NumberPerPage ?? 10);
            int pageNumber = page ?? 1;

            IQueryable<ProductionOrder> query = _context.ProductionOrders
                .Include(p => p.Medicine)
                .Include(p => p.Unit);

            if (!string.IsNullOrWhiteSpace(searchKey))
            {
                query = query.Where(p => p.OrderNumber.Contains(searchKey) ||
                                     p.Medicine.BrandName.Contains(searchKey))
                            .OrderBy(p => p.OrderNumber);
            }

            int total = await query.CountAsync();
            var items = await query
                .OrderByDescending(p => p.TargetDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var paginatedList = new PaginatedList<ProductionOrder>(items, total, pageNumber, pageSize);

            return View(paginatedList);
        }

        // GET: ProductionOrder/AddProductionOrder
        public async Task<IActionResult> AddProductionOrder()
        {
            await PopulateProductionOrderDropdowns();
            return View(new ProductionOrder
            {
                TargetDate = DateOnly.FromDateTime(DateTime.Now.AddDays(7)),
                Status = "Planned",
                Priority = 3
            });
        }

        // POST: ProductionOrder/AddProductionOrder
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddProductionOrder(ProductionOrder productionOrder)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    if (await _context.ProductionOrders.AnyAsync(p => p.OrderNumber == productionOrder.OrderNumber))
                    {
                        TempData["ErrorMessage"] = "سفارش تولید با این شماره قبلاً ثبت شده است";
                        await PopulateProductionOrderDropdowns();
                        return View(productionOrder);
                    }

                    _context.Add(productionOrder);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "سفارش تولید جدید با موفقیت ایجاد شد";
                    return RedirectToAction(nameof(ProductionOrderList));
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "خطا در ایجاد سفارش تولید: " + ex.Message;
                }
            }

            await PopulateProductionOrderDropdowns();
            return View(productionOrder);
        }

        // GET: ProductionOrder/EditProductionOrder/5
        public async Task<IActionResult> EditProductionOrder(Guid id)
        {
            var productionOrder = await _context.ProductionOrders.FindAsync(id);
            if (productionOrder == null)
            {
                return NotFound();
            }

            await PopulateProductionOrderDropdowns();
            return View(productionOrder);
        }

        // POST: ProductionOrder/EditProductionOrder/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProductionOrder(Guid id, ProductionOrder productionOrder)
        {
            if (id != productionOrder.OrderId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (await _context.ProductionOrders.AnyAsync(p =>
                        p.OrderId != id &&
                        p.OrderNumber == productionOrder.OrderNumber))
                    {
                        TempData["ErrorMessage"] = "سفارش تولید با این شماره قبلاً ثبت شده است";
                        await PopulateProductionOrderDropdowns();
                        return View(productionOrder);
                    }

                    _context.Update(productionOrder);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "اطلاعات سفارش تولید با موفقیت به‌روزرسانی شد";
                    return RedirectToAction(nameof(ProductionOrderList));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductionOrderExists(productionOrder.OrderId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            await PopulateProductionOrderDropdowns();
            return View(productionOrder);
        }

        // POST: ProductionOrder/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var productionOrder = await _context.ProductionOrders.FindAsync(id);
            if (productionOrder == null)
            {
                TempData["ErrorMessage"] = "سفارش تولید مورد نظر یافت نشد";
                return RedirectToAction(nameof(ProductionOrderList));
            }

            // Check if order has any batches
            bool hasBatches = await _context.FinishedGoodsBatches.AnyAsync(f => f.OrderId == id);
            if (hasBatches)
            {
                TempData["ErrorMessage"] = "این سفارش تولید دارای دسته‌های محصول است و قابل حذف نیست";
                return RedirectToAction(nameof(ProductionOrderList));
            }

            try
            {
                _context.ProductionOrders.Remove(productionOrder);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "سفارش تولید با موفقیت حذف شد";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "خطا در حذف سفارش تولید: " + ex.Message;
            }

            return RedirectToAction(nameof(ProductionOrderList));
        }

        private bool ProductionOrderExists(Guid id)
        {
            return _context.ProductionOrders.Any(e => e.OrderId == id);
        }

        private async Task PopulateProductionOrderDropdowns()
        {
            // Active medicines
            var medicines = await _context.Medicines
                .Where(m => m.IsActive == true)
                .OrderBy(m => m.BrandName)
                .ToListAsync();

            ViewBag.Medicines = new SelectList(medicines, "MedicineId", "BrandName");

            // Active units
            var units = await _context.Units
                .Where(u => u.IsActive == true)
                .OrderBy(u => u.UnitName)
                .ToListAsync();

            ViewBag.Units = new SelectList(units, "UnitId", "UnitName");

            // Statuses
            var statuses = new List<SelectListItem>
            {
                new SelectListItem { Value = "Planned", Text = "برنامه‌ریزی شده" },
                new SelectListItem { Value = "In Progress", Text = "در حال انجام" },
                new SelectListItem { Value = "Completed", Text = "تکمیل شده" },
                new SelectListItem { Value = "Cancelled", Text = "لغو شده" }
            };

            ViewBag.Statuses = new SelectList(statuses, "Value", "Text");

            // Priorities
            var priorities = new List<SelectListItem>
            {
                new SelectListItem { Value = "1", Text = "فوری (1)" },
                new SelectListItem { Value = "2", Text = "بالا (2)" },
                new SelectListItem { Value = "3", Text = "متوسط (3)" },
                new SelectListItem { Value = "4", Text = "پایین (4)" }
            };

            ViewBag.Priorities = new SelectList(priorities, "Value", "Text");
        }
    }
}