using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mehrsam_Darou.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using static Mehrsam_Darou.Helper.Helper;

namespace Mehrsam_Darou.Controllers
{
    public class PurchaseOrderController : BaseController
    {
        private readonly DarouAppContext _context;

        public PurchaseOrderController(DarouAppContext context) : base(context)
        {
            _context = context;
        }

        // GET: PurchaseOrder/List
        public async Task<IActionResult> PurchaseOrderList(int? page, string searchKey, string statusFilter)
        {
            var setting = await ReadSettingAsync(_context);
            int pageSize = Convert.ToInt32(setting.NumberPerPage ?? 10);
            int pageNumber = page ?? 1;

            IQueryable<PurchaseOrder> query = _context.PurchaseOrders
                .Include(po => po.Supplier)
                .Include(po => po.PurchaseOrderItems)
                .ThenInclude(poi => poi.Material)
                .Include(po => po.PurchaseOrderItems)
                .ThenInclude(poi => poi.Unit);

            if (!string.IsNullOrWhiteSpace(searchKey))
            {
                query = query.Where(po => po.PoNumber.Contains(searchKey) ||
                                     po.Supplier.SupplierName.Contains(searchKey) ||
                                     po.Notes.Contains(searchKey));
            }

            if (!string.IsNullOrWhiteSpace(statusFilter))
            {
                query = query.Where(po => po.Status == statusFilter);
            }

            query = query.OrderByDescending(po => po.OrderDate);

            int total = await query.CountAsync();
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var paginatedList = new PaginatedList<PurchaseOrder>(items, total, pageNumber, pageSize);

            ViewBag.StatusFilter = statusFilter;
            ViewBag.SearchKey = searchKey;

            return View(paginatedList);
        }

        // GET: PurchaseOrder/Add
        public async Task<IActionResult> AddPurchaseOrder()
        {
            ViewBag.Suppliers = await _context.Suppliers.Where(s => s.IsActive == true).ToListAsync();
            ViewBag.Materials = await _context.RawMaterials
                .Include(m => m.Unit)
                .Where(m => m.IsActive == true)
                .ToListAsync();
            ViewBag.Units = await _context.Units.Where(u => u.IsActive == true).ToListAsync();

            var newOrder = new PurchaseOrder
            {
                OrderDate =DateOnly.FromDateTime(DateTime.Today),
                Status = "پیش‌نویس",
                Currency = "IRR",
                CreatedDate = DateTime.Now
            };

            return View(newOrder);
        }

        // POST: PurchaseOrder/Add
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddPurchaseOrder(PurchaseOrder purchaseOrder, List<PurchaseOrderItem> items)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    if (await _context.PurchaseOrders.AnyAsync(po => po.PoNumber == purchaseOrder.PoNumber))
                    {
                        TempData["ErrorMessage"] = "سفارش خرید با این شماره قبلاً ثبت شده است";
                        return await AddWithViewData(purchaseOrder);
                    }

                    purchaseOrder.PurchaseOrderId = Guid.NewGuid();
                    purchaseOrder.CreatedDate = DateTime.Now;
                    purchaseOrder.TotalAmount = items.Sum(i => i.TotalPrice);

                    _context.Add(purchaseOrder);

                    foreach (var item in items.Where(i => i.MaterialId != Guid.Empty))
                    {
                        item.PoItemId = Guid.NewGuid();
                        item.PurchaseOrderId = purchaseOrder.PurchaseOrderId;
                        item.ReceivedQuantity = 0;
                        _context.Add(item);
                    }

                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "سفارش خرید جدید با موفقیت ایجاد شد";
                    return RedirectToAction(nameof(PurchaseOrderList));
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "خطا در ایجاد سفارش خرید: " + ex.Message;
                }
            }

            return await AddWithViewData(purchaseOrder);
        }

        private async Task<IActionResult> AddWithViewData(PurchaseOrder purchaseOrder)
        {
            ViewBag.Suppliers = await _context.Suppliers.Where(s => s.IsActive == true).ToListAsync();
            ViewBag.Materials = await _context.RawMaterials
                .Include(m => m.Unit)
                .Where(m => m.IsActive == true)
                .ToListAsync();
            ViewBag.Units = await _context.Units.Where(u => u.IsActive == true).ToListAsync();

            return View("Add", purchaseOrder);
        }

        // GET: PurchaseOrder/Edit/5
        public async Task<IActionResult> EditPurchaseOrder(Guid id)
        {
            var purchaseOrder = await _context.PurchaseOrders
                .Include(po => po.Supplier)
                .Include(po => po.PurchaseOrderItems)
                .ThenInclude(poi => poi.Material)
                .Include(po => po.PurchaseOrderItems)
                .ThenInclude(poi => poi.Unit)
                .FirstOrDefaultAsync(po => po.PurchaseOrderId == id);

            if (purchaseOrder == null)
            {
                return NotFound();
            }

            if (purchaseOrder.Status != "پیش‌نویس")
            {
                TempData["ErrorMessage"] = "فقط سفارشات با وضعیت پیش‌نویس قابل ویرایش هستند";
                return RedirectToAction(nameof(PurchaseOrderList));
            }

            ViewBag.Suppliers = await _context.Suppliers.Where(s => s.IsActive == true).ToListAsync();
            ViewBag.Materials = await _context.RawMaterials
                .Include(m => m.Unit)
                .Where(m => m.IsActive == true)
                .ToListAsync();
            ViewBag.Units = await _context.Units.Where(u => u.IsActive == true).ToListAsync();

            return View(purchaseOrder);
        }

        // POST: PurchaseOrder/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPurchaseOrder(Guid id, PurchaseOrder purchaseOrder, List<PurchaseOrderItem> items)
        {
            if (id != purchaseOrder.PurchaseOrderId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (await _context.PurchaseOrders.AnyAsync(po =>
                        po.PurchaseOrderId != id &&
                        po.PoNumber == purchaseOrder.PoNumber))
                    {
                        TempData["ErrorMessage"] = "سفارش خرید با این شماره قبلاً ثبت شده است";
                        return await EditWithViewData(purchaseOrder);
                    }

                    var existingOrder = await _context.PurchaseOrders
                        .Include(po => po.PurchaseOrderItems)
                        .FirstOrDefaultAsync(po => po.PurchaseOrderId == id);

                    if (existingOrder == null)
                    {
                        return NotFound();
                    }

                    if (existingOrder.Status != "پیش‌نویس")
                    {
                        TempData["ErrorMessage"] = "فقط سفارشات با وضعیت پیش‌نویس قابل ویرایش هستند";
                        return await EditWithViewData(purchaseOrder);
                    }

                    // Update main order properties
                    existingOrder.PoNumber = purchaseOrder.PoNumber;
                    existingOrder.SupplierId = purchaseOrder.SupplierId;
                    existingOrder.OrderDate = purchaseOrder.OrderDate;
                    existingOrder.ExpectedDeliveryDate = purchaseOrder.ExpectedDeliveryDate;
                    existingOrder.Currency = purchaseOrder.Currency;
                    existingOrder.Status = purchaseOrder.Status;
                    existingOrder.Notes = purchaseOrder.Notes;
                    existingOrder.TotalAmount = items.Sum(i => i.TotalPrice);

                    // Remove existing items not in the new list
                    var itemsToRemove = existingOrder.PurchaseOrderItems
                        .Where(existingItem => !items.Any(newItem => newItem.PoItemId == existingItem.PoItemId))
                        .ToList();

                    foreach (var item in itemsToRemove)
                    {
                        _context.PurchaseOrderItems.Remove(item);
                    }

                    // Update or add items
                    foreach (var item in items.Where(i => i.MaterialId != Guid.Empty))
                    {
                        var existingItem = existingOrder.PurchaseOrderItems
                            .FirstOrDefault(i => i.PoItemId == item.PoItemId);

                        if (existingItem != null)
                        {
                            // Update existing item
                            existingItem.MaterialId = item.MaterialId;
                            existingItem.Quantity = item.Quantity;
                            existingItem.UnitId = item.UnitId;
                            existingItem.UnitPrice = item.UnitPrice;
                            existingItem.TotalPrice = item.TotalPrice;
                            existingItem.Notes = item.Notes;
                        }
                        else
                        {
                            // Add new item
                            item.PoItemId = Guid.NewGuid();
                            item.PurchaseOrderId = existingOrder.PurchaseOrderId;
                            item.ReceivedQuantity = 0;
                            _context.Add(item);
                        }
                    }

                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "سفارش خرید با موفقیت به‌روزرسانی شد";
                    return RedirectToAction(nameof(PurchaseOrderList));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PurchaseOrderExists(purchaseOrder.PurchaseOrderId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            return await EditWithViewData(purchaseOrder);
        }

        private async Task<IActionResult> EditWithViewData(PurchaseOrder purchaseOrder)
        {
            ViewBag.Suppliers = await _context.Suppliers.Where(s => s.IsActive == true).ToListAsync();
            ViewBag.Materials = await _context.RawMaterials
                .Include(m => m.Unit)
                .Where(m => m.IsActive == true)
                .ToListAsync();
            ViewBag.Units = await _context.Units.Where(u => u.IsActive == true).ToListAsync();

            return View("Edit", purchaseOrder);
        }

        // POST: PurchaseOrder/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var purchaseOrder = await _context.PurchaseOrders
                .Include(po => po.PurchaseOrderItems)
                .Include(po => po.PurchaseInvoices)
                .FirstOrDefaultAsync(po => po.PurchaseOrderId == id);

            if (purchaseOrder == null)
            {
                TempData["ErrorMessage"] = "سفارش خرید مورد نظر یافت نشد";
                return RedirectToAction(nameof(PurchaseOrderList));
            }

            if (purchaseOrder.Status != "پیش‌نویس")
            {
                TempData["ErrorMessage"] = "فقط سفارشات با وضعیت پیش‌نویس قابل حذف هستند";
                return RedirectToAction(nameof(PurchaseOrderList));
            }

            if (purchaseOrder.PurchaseInvoices.Any())
            {
                TempData["ErrorMessage"] = "این سفارش خرید دارای فاکتور است و قابل حذف نیست";
                return RedirectToAction(nameof(PurchaseOrderList));
            }

            try
            {
                _context.PurchaseOrderItems.RemoveRange(purchaseOrder.PurchaseOrderItems);
                _context.PurchaseOrders.Remove(purchaseOrder);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "سفارش خرید با موفقیت حذف شد";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "خطا در حذف سفارش خرید: " + ex.Message;
            }

            return RedirectToAction(nameof(PurchaseOrderList));
        }

        private bool PurchaseOrderExists(Guid id)
        {
            return _context.PurchaseOrders.Any(e => e.PurchaseOrderId == id);
        }
    }
}