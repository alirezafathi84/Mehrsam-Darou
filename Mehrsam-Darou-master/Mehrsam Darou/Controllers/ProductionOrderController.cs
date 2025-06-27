using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mehrsam_Darou.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using static Mehrsam_Darou.Helper.Helper;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.Reflection;

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
                Status = "برنامه‌ریزی شده", // Changed to Persian to match database constraint
                Priority = 3
            });
        }

        // POST: ProductionOrder/AddProductionOrder
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddProductionOrder(ProductionOrder productionOrder)
        {
            // Remove ModelState errors for navigation properties
            ModelState.Remove("Unit");
            ModelState.Remove("Medicine");

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

                    // Clear navigation properties to avoid conflicts
                    productionOrder.Medicine = null;
                    productionOrder.Unit = null;

                    // Set OrderId if it's empty (for new records)
                    if (productionOrder.OrderId == Guid.Empty)
                    {
                        productionOrder.OrderId = Guid.NewGuid();
                    }

                    _context.Add(productionOrder);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "سفارش تولید جدید با موفقیت ایجاد شد";
                    return RedirectToAction(nameof(ProductionOrderList));
                }
                catch (Exception ex)
                {
                    // Get detailed error information
                    var innerException = ex.InnerException?.Message ?? ex.Message;
                    var detailedError = $"خطا در ایجاد سفارش تولید: {ex.Message}";

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

            // Remove ModelState errors for navigation properties
            ModelState.Remove("Unit");
            ModelState.Remove("Medicine");

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

                    // Clear navigation properties to avoid conflicts
                    productionOrder.Medicine = null;
                    productionOrder.Unit = null;

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
                catch (Exception ex)
                {
                    // Get detailed error information
                    var innerException = ex.InnerException?.Message ?? ex.Message;
                    var detailedError = $"خطا در به‌روزرسانی سفارش تولید: {ex.Message}";

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
            try
            {
                // Active medicines - Create SelectListItem manually to avoid property name issues
                var medicines = await _context.Medicines
                    .Where(m => m.IsActive == true)
                    .OrderBy(m => m.BrandName)
                    .ToListAsync();

                var medicineItems = new List<SelectListItem>();
                if (medicines != null && medicines.Any())
                {
                    foreach (var medicine in medicines)
                    {
                        // Try different possible ID property names
                        var idValue = GetEntityId(medicine);
                        if (idValue != null)
                        {
                            medicineItems.Add(new SelectListItem
                            {
                                Value = idValue.ToString(),
                                Text = medicine.BrandName
                            });
                        }
                    }
                }
                ViewBag.Medicines = new SelectList(medicineItems, "Value", "Text");

                // Active units - Create SelectListItem manually to avoid property name issues
                var units = await _context.Units
                    .Where(u => u.IsActive == true)
                    .OrderBy(u => u.UnitName)
                    .ToListAsync();

                var unitItems = new List<SelectListItem>();
                if (units != null && units.Any())
                {
                    foreach (var unit in units)
                    {
                        // Try different possible ID property names
                        var idValue = GetEntityId(unit);
                        if (idValue != null)
                        {
                            unitItems.Add(new SelectListItem
                            {
                                Value = idValue.ToString(),
                                Text = unit.UnitName
                            });
                        }
                    }
                }
                ViewBag.Units = new SelectList(unitItems, "Value", "Text");
            }
            catch (Exception)
            {
                // Fallback to empty lists if database queries fail
                ViewBag.Medicines = new SelectList(new List<SelectListItem>(), "Value", "Text");
                ViewBag.Units = new SelectList(new List<SelectListItem>(), "Value", "Text");
            }

            // Statuses - Fixed to match database CHECK constraint (Persian values)
            var statuses = new List<SelectListItem>
            {
                new SelectListItem { Value = "برنامه‌ریزی شده", Text = "برنامه‌ریزی شده" },
                new SelectListItem { Value = "در حال اجرا", Text = "در حال اجرا" },
                new SelectListItem { Value = "تکمیل شده", Text = "تکمیل شده" },
                new SelectListItem { Value = "لغو شده", Text = "لغو شده" }
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

        private object GetEntityId(object entity)
        {
            // Try to get ID using reflection to handle different property names
            var type = entity.GetType();

            // Common ID property names to try
            string[] possibleIdNames = { "Id", "MedicineId", "UnitId", type.Name + "Id" };

            foreach (var propName in possibleIdNames)
            {
                var property = type.GetProperty(propName);
                if (property != null)
                {
                    return property.GetValue(entity);
                }
            }

            return null;
        }
    }
}