using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Mehrsam_Darou.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using static Mehrsam_Darou.Helper.Helper;

namespace Mehrsam_Darou.Controllers
{
    public class SalesOrderController : BaseController
    {
        private readonly DarouAppContext _context;

        public SalesOrderController(DarouAppContext context) : base(context)
        {
            _context = context;
        }

        // GET: SalesOrder/SalesOrderList
        public async Task<IActionResult> SalesOrderList(int? page, string searchKey, string status)
        {
            var setting = await ReadSettingAsync(_context);
            int pageSize = Convert.ToInt32(setting.NumberPerPage ?? 10);
            int pageNumber = page ?? 1;

            IQueryable<SalesOrder> query = _context.SalesOrders
                .Include(s => s.Customer)
                .Include(s => s.SalesOrderItems)
                .ThenInclude(i => i.Medicine);

            if (!string.IsNullOrWhiteSpace(searchKey))
            {
                query = query.Where(s => s.SoNumber.Contains(searchKey) ||
                                     s.Customer.CustomerName.Contains(searchKey) ||
                                     s.Customer.CustomerCode.Contains(searchKey) ||
                                     s.Notes.Contains(searchKey));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(s => s.Status == status);
            }

            query = query.OrderByDescending(s => s.CreatedDate);

            int total = await query.CountAsync();
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var paginatedList = new PaginatedList<SalesOrder>(items, total, pageNumber, pageSize);

            // Pass status options for filter
            ViewBag.StatusOptions = new SelectList(new[]
            {
                new { Value = "", Text = "همه وضعیت‌ها" },
                new { Value = "پیش‌نویس", Text = "پیش‌نویس" },
                new { Value = "تایید شده", Text = "تایید شده" },
                new { Value = "در حال تولید", Text = "در حال تولید" },
                new { Value = "آماده ارسال", Text = "آماده ارسال" },
                new { Value = "ارسال شده", Text = "ارسال شده" },
                new { Value = "تحویل داده شده", Text = "تحویل داده شده" },
                new { Value = "لغو شده", Text = "لغو شده" }
            }, "Value", "Text", status);

            return View(paginatedList);
        }

        // GET: SalesOrder/AddSalesOrder
        public async Task<IActionResult> AddSalesOrder()
        {
            await PopulateDropdowns();

            var salesOrder = new SalesOrder
            {
                OrderDate = DateOnly.FromDateTime(DateTime.Now),
                Status = "پیش‌نویس",
                Priority = 3,
                Currency = "IRR",
                TaxAmount = 0,
                DiscountAmount = 0,
                TotalAmount = 0,
                NetAmount = 0,
                CreatedDate = DateTime.Now
            };

            // Generate next SO number
            var lastOrder = await _context.SalesOrders
                .OrderByDescending(s => s.CreatedDate)
                .FirstOrDefaultAsync();

            if (lastOrder != null && lastOrder.SoNumber.StartsWith("SO"))
            {
                if (int.TryParse(lastOrder.SoNumber.Substring(2), out int lastNumber))
                {
                    salesOrder.SoNumber = $"SO{(lastNumber + 1):D6}";
                }
                else
                {
                    salesOrder.SoNumber = "SO000001";
                }
            }
            else
            {
                salesOrder.SoNumber = "SO000001";
            }

            return View(salesOrder);
        }

        // POST: SalesOrder/AddSalesOrder
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddSalesOrder(SalesOrder salesOrder)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    if (await _context.SalesOrders.AnyAsync(s => s.SoNumber == salesOrder.SoNumber))
                    {
                        TempData["ErrorMessage"] = "سفارش فروش با این شماره قبلاً ثبت شده است";
                        await PopulateDropdowns();
                        return View(salesOrder);
                    }

                    salesOrder.SalesOrderId = Guid.NewGuid();
                    salesOrder.CreatedDate = DateTime.Now;

                    // Initialize amounts
                    salesOrder.TotalAmount = 0;
                    salesOrder.TaxAmount = salesOrder.TaxAmount ?? 0;
                    salesOrder.DiscountAmount = salesOrder.DiscountAmount ?? 0;
                    salesOrder.NetAmount = 0;

                    _context.Add(salesOrder);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "سفارش فروش جدید با موفقیت ایجاد شد";
                    return RedirectToAction(nameof(EditSalesOrder), new { id = salesOrder.SalesOrderId });
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "خطا در ایجاد سفارش فروش: " + ex.Message;
                }
            }

            await PopulateDropdowns();
            return View(salesOrder);
        }

        // GET: SalesOrder/EditSalesOrder/5
        public async Task<IActionResult> EditSalesOrder(Guid id)
        {
            var salesOrder = await _context.SalesOrders
                .Include(s => s.Customer)
                .Include(s => s.SalesOrderItems)
                .ThenInclude(i => i.Medicine)
                .Include(s => s.SalesOrderItems)
                .ThenInclude(i => i.Unit)
                .FirstOrDefaultAsync(s => s.SalesOrderId == id);

            if (salesOrder == null)
            {
                return NotFound();
            }

            await PopulateDropdowns();
            return View(salesOrder);
        }

        // POST: SalesOrder/EditSalesOrder/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSalesOrder(Guid id, SalesOrder salesOrder)
        {
            if (id != salesOrder.SalesOrderId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (await _context.SalesOrders.AnyAsync(s =>
                        s.SalesOrderId != id &&
                        s.SoNumber == salesOrder.SoNumber))
                    {
                        TempData["ErrorMessage"] = "سفارش فروش با این شماره قبلاً ثبت شده است";
                        await PopulateDropdowns();
                        return View(salesOrder);
                    }

                    var existingSalesOrder = await _context.SalesOrders
                        .Include(s => s.SalesOrderItems)
                        .FirstOrDefaultAsync(s => s.SalesOrderId == id);

                    if (existingSalesOrder == null)
                    {
                        return NotFound();
                    }

                    // Keep the original creation date
                    salesOrder.CreatedDate = existingSalesOrder.CreatedDate;

                    // Update values
                    _context.Entry(existingSalesOrder).CurrentValues.SetValues(salesOrder);

                    // Recalculate totals based on existing items and new tax/discount
                    await RecalculateOrderTotals(existingSalesOrder);

                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "اطلاعات سفارش فروش با موفقیت به‌روزرسانی شد";
                    return RedirectToAction(nameof(EditSalesOrder), new { id = id });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SalesOrderExists(salesOrder.SalesOrderId))
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
            return View(salesOrder);
        }

        // POST: SalesOrder/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var salesOrder = await _context.SalesOrders.FindAsync(id);
            if (salesOrder == null)
            {
                TempData["ErrorMessage"] = "سفارش فروش مورد نظر یافت نشد";
                return RedirectToAction(nameof(SalesOrderList));
            }

            // Check if sales order has any invoices or shipments
            bool hasInvoices = await _context.SalesInvoices.AnyAsync(i => i.SalesOrderId == id);
            bool hasShipments = await _context.Shipments.AnyAsync(s => s.SalesOrderId == id);

            if (hasInvoices || hasShipments)
            {
                TempData["ErrorMessage"] = "این سفارش فروش دارای فاکتور یا حمل و نقل است و قابل حذف نیست";
                return RedirectToAction(nameof(SalesOrderList));
            }

            try
            {
                // Delete related order items first
                var orderItems = await _context.SalesOrderItems
                    .Where(i => i.SalesOrderId == id)
                    .ToListAsync();

                _context.SalesOrderItems.RemoveRange(orderItems);
                _context.SalesOrders.Remove(salesOrder);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "سفارش فروش با موفقیت حذف شد";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "خطا در حذف سفارش فروش: " + ex.Message;
            }

            return RedirectToAction(nameof(SalesOrderList));
        }

        // GET: SalesOrder/Details/5
        public async Task<IActionResult> Details(Guid id)
        {
            var salesOrder = await _context.SalesOrders
                .Include(s => s.Customer)
                .Include(s => s.SalesOrderItems)
                .ThenInclude(i => i.Medicine)
                .Include(s => s.SalesOrderItems)
                .ThenInclude(i => i.Unit)
                .FirstOrDefaultAsync(s => s.SalesOrderId == id);

            if (salesOrder == null)
            {
                return NotFound();
            }

            return View(salesOrder);
        }

        // GET: SalesOrder/Print/5
        public async Task<IActionResult> Print(Guid id)
        {
            var salesOrder = await _context.SalesOrders
                .Include(s => s.Customer)
                .Include(s => s.SalesOrderItems)
                .ThenInclude(i => i.Medicine)
                .Include(s => s.SalesOrderItems)
                .ThenInclude(i => i.Unit)
                .FirstOrDefaultAsync(s => s.SalesOrderId == id);

            if (salesOrder == null)
            {
                return NotFound();
            }

            return View(salesOrder);
        }

        // AJAX: Add Order Item
        [HttpPost]
        public async Task<IActionResult> AddOrderItem(Guid salesOrderId, Guid medicineId, decimal quantity, Guid unitId, decimal unitPrice, decimal discountPercent = 0)
        {
            try
            {
                var salesOrder = await _context.SalesOrders.FindAsync(salesOrderId);
                if (salesOrder == null)
                {
                    return Json(new { success = false, message = "سفارش فروش یافت نشد" });
                }

                var medicine = await _context.Medicines.FindAsync(medicineId);
                var unit = await _context.Units.FindAsync(unitId);

                if (medicine == null || unit == null)
                {
                    return Json(new { success = false, message = "دارو یا واحد یافت نشد" });
                }

                var totalPrice = (quantity * unitPrice) * (1 - discountPercent / 100);

                var orderItem = new SalesOrderItem
                {
                    SoItemId = Guid.NewGuid(),
                    SalesOrderId = salesOrderId,
                    MedicineId = medicineId,
                    Quantity = quantity,
                    UnitId = unitId,
                    UnitPrice = unitPrice,
                    DiscountPercent = discountPercent,
                    TotalPrice = totalPrice,
                    ShippedQuantity = 0
                };

                _context.SalesOrderItems.Add(orderItem);
                await _context.SaveChangesAsync();

                // Update order totals
                await UpdateOrderTotals(salesOrderId);

                return Json(new { success = true, message = "آیتم با موفقیت اضافه شد" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "خطا در افزودن آیتم: " + ex.Message });
            }
        }

        // AJAX: Remove Order Item
        [HttpPost]
        public async Task<IActionResult> RemoveOrderItem(Guid itemId)
        {
            try
            {
                var orderItem = await _context.SalesOrderItems.FindAsync(itemId);
                if (orderItem == null)
                {
                    return Json(new { success = false, message = "آیتم یافت نشد" });
                }

                var salesOrderId = orderItem.SalesOrderId;
                _context.SalesOrderItems.Remove(orderItem);
                await _context.SaveChangesAsync();

                // Update order totals
                await UpdateOrderTotals(salesOrderId);

                return Json(new { success = true, message = "آیتم با موفقیت حذف شد" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "خطا در حذف آیتم: " + ex.Message });
            }
        }

        // AJAX: Update Tax and Discount
        [HttpPost]
        public async Task<IActionResult> UpdateTaxDiscount(Guid salesOrderId, decimal taxAmount, decimal discountAmount)
        {
            try
            {
                var salesOrder = await _context.SalesOrders
                    .Include(s => s.SalesOrderItems)
                    .FirstOrDefaultAsync(s => s.SalesOrderId == salesOrderId);

                if (salesOrder == null)
                {
                    return Json(new { success = false, message = "سفارش فروش یافت نشد" });
                }

                salesOrder.TaxAmount = taxAmount;
                salesOrder.DiscountAmount = discountAmount;

                await RecalculateOrderTotals(salesOrder);
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = "مالیات و تخفیف به‌روزرسانی شد",
                    totalAmount = salesOrder.TotalAmount?.ToString("N2"),
                    taxAmount = salesOrder.TaxAmount?.ToString("N2"),
                    discountAmount = salesOrder.DiscountAmount?.ToString("N2"),
                    netAmount = salesOrder.NetAmount?.ToString("N2")
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "خطا در به‌روزرسانی: " + ex.Message });
            }
        }

        private async Task UpdateOrderTotals(Guid salesOrderId)
        {
            var salesOrder = await _context.SalesOrders
                .Include(s => s.SalesOrderItems)
                .FirstOrDefaultAsync(s => s.SalesOrderId == salesOrderId);

            if (salesOrder != null)
            {
                await RecalculateOrderTotals(salesOrder);
                await _context.SaveChangesAsync();
            }
        }

        private async Task RecalculateOrderTotals(SalesOrder salesOrder)
        {
            // Calculate subtotal from items
            var subtotal = salesOrder.SalesOrderItems?.Sum(i => i.TotalPrice) ?? 0;

            // Set total amount (subtotal)
            salesOrder.TotalAmount = subtotal;

            // Get tax and discount amounts (preserve existing values if not null)
            var taxAmount = salesOrder.TaxAmount ?? 0;
            var discountAmount = salesOrder.DiscountAmount ?? 0;

            // Calculate net amount: subtotal + tax - discount
            salesOrder.NetAmount = subtotal + taxAmount - discountAmount;

            // Ensure NetAmount is not negative
            if (salesOrder.NetAmount < 0)
            {
                salesOrder.NetAmount = 0;
            }
        }

        private async Task PopulateDropdowns()
        {
            ViewBag.Customers = new SelectList(
                await _context.Customers
                    .Where(c => c.IsActive == true)
                    .OrderBy(c => c.CustomerName)
                    .ToListAsync(),
                "CustomerId", "CustomerName");

            ViewBag.Medicines = new SelectList(
                await _context.Medicines
                    .Where(m => m.IsActive == true)
                    .OrderBy(m => m.BrandName)
                    .ToListAsync(),
                "MedicineId", "BrandName");

            ViewBag.Units = new SelectList(
                await _context.Units
                    .Where(u => u.IsActive == true)
                    .OrderBy(u => u.UnitName)
                    .ToListAsync(),
                "UnitId", "UnitName");

            ViewBag.StatusOptions = new SelectList(new[]
            {
                new { Value = "پیش‌نویس", Text = "پیش‌نویس" },
                new { Value = "تایید شده", Text = "تایید شده" },
                new { Value = "در حال تولید", Text = "در حال تولید" },
                new { Value = "آماده ارسال", Text = "آماده ارسال" },
                new { Value = "ارسال شده", Text = "ارسال شده" },
                new { Value = "تحویل داده شده", Text = "تحویل داده شده" },
                new { Value = "لغو شده", Text = "لغو شده" }
            }, "Value", "Text");

            ViewBag.PriorityOptions = new SelectList(new[]
            {
                new { Value = 1, Text = "بالا" },
                new { Value = 2, Text = "متوسط" },
                new { Value = 3, Text = "عادی" },
                new { Value = 4, Text = "پایین" }
            }, "Value", "Text");
        }

        private bool SalesOrderExists(Guid id)
        {
            return _context.SalesOrders.Any(e => e.SalesOrderId == id);
        }
    }
}