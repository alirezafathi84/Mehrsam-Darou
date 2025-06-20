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

        // GET: PurchaseOrder/PurchaseOrderList
        public async Task<IActionResult> PurchaseOrderList(int? page, string searchKey)
        {
            var setting = await ReadSettingAsync(_context);
            int pageSize = Convert.ToInt32(setting.NumberPerPage ?? 10);
            int pageNumber = page ?? 1;

            IQueryable<PurchaseOrder> query = _context.PurchaseOrders
                .Include(p => p.Supplier);

            if (!string.IsNullOrWhiteSpace(searchKey))
            {
                query = query.Where(p => p.PoNumber.Contains(searchKey) ||
                                     p.Supplier.SupplierName.Contains(searchKey) ||
                                     p.Status.Contains(searchKey))
                            .OrderByDescending(p => p.CreatedDate);
            }

            int total = await query.CountAsync();
            var items = await query
                .OrderByDescending(p => p.CreatedDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var paginatedList = new PaginatedList<PurchaseOrder>(items, total, pageNumber, pageSize);

            return View(paginatedList);
        }

        // GET: PurchaseOrder/AddPurchaseOrder
        public async Task<IActionResult> AddPurchaseOrder()
        {
            await LoadSuppliers();

            return View(new PurchaseOrder
            {
                OrderDate = DateOnly.FromDateTime(DateTime.Now),
                Status = "پیش‌نویس",
                Currency = "IRR",
                CreatedDate = DateTime.Now
            });
        }

        // POST: PurchaseOrder/AddPurchaseOrder
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddPurchaseOrder(PurchaseOrder purchaseOrder)
        {
            // Remove Supplier from ModelState validation since we only need SupplierId
            ModelState.Remove("Supplier");

            if (ModelState.IsValid)
            {
                try
                {
                    if (await _context.PurchaseOrders.AnyAsync(p => p.PoNumber == purchaseOrder.PoNumber))
                    {
                        TempData["ErrorMessage"] = "سفارش خرید با این شماره قبلاً ثبت شده است";
                        await LoadSuppliers();
                        return View(purchaseOrder);
                    }

                    purchaseOrder.PurchaseOrderId = Guid.NewGuid();
                    purchaseOrder.CreatedDate = DateTime.Now;

                    _context.Add(purchaseOrder);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "سفارش خرید جدید با موفقیت ایجاد شد";
                    return RedirectToAction(nameof(PurchaseOrderList));
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "خطا در ایجاد سفارش خرید: " + ex.Message;
                }
            }

            await LoadSuppliers();
            return View(purchaseOrder);
        }

        // GET: PurchaseOrder/EditPurchaseOrder/5
        public async Task<IActionResult> EditPurchaseOrder(Guid id)
        {
            var purchaseOrder = await _context.PurchaseOrders
                .Include(p => p.Supplier)
                .FirstOrDefaultAsync(p => p.PurchaseOrderId == id);

            if (purchaseOrder == null)
            {
                return NotFound();
            }

            await LoadSuppliers();

            // Load purchase order items for display
            var items = await _context.PurchaseOrderItems
                .Include(poi => poi.Material)
                .Include(poi => poi.Unit)
                .Where(poi => poi.PurchaseOrderId == id)
                .OrderBy(poi => poi.Material.MaterialName)
                .ToListAsync();

            ViewBag.PurchaseOrderItems = items;

            return View(purchaseOrder);
        }

        // POST: PurchaseOrder/EditPurchaseOrder/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPurchaseOrder(Guid id, PurchaseOrder purchaseOrder)
        {
            if (id != purchaseOrder.PurchaseOrderId)
            {
                return NotFound();
            }

            // Remove Supplier from ModelState validation since we only need SupplierId
            ModelState.Remove("Supplier");

            if (ModelState.IsValid)
            {
                try
                {
                    if (await _context.PurchaseOrders.AnyAsync(p =>
                        p.PurchaseOrderId != id &&
                        p.PoNumber == purchaseOrder.PoNumber))
                    {
                        TempData["ErrorMessage"] = "سفارش خرید با این شماره قبلاً ثبت شده است";
                        await LoadSuppliers();
                        return View(purchaseOrder);
                    }

                    var existingPurchaseOrder = await _context.PurchaseOrders.FindAsync(id);
                    if (existingPurchaseOrder == null)
                    {
                        return NotFound();
                    }

                    // Keep the original creation date
                    purchaseOrder.CreatedDate = existingPurchaseOrder.CreatedDate;

                    _context.Entry(existingPurchaseOrder).CurrentValues.SetValues(purchaseOrder);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "اطلاعات سفارش خرید با موفقیت به‌روزرسانی شد";
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

            await LoadSuppliers();
            return View(purchaseOrder);
        }

        // POST: PurchaseOrder/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var purchaseOrder = await _context.PurchaseOrders.FindAsync(id);
            if (purchaseOrder == null)
            {
                TempData["ErrorMessage"] = "سفارش خرید مورد نظر یافت نشد";
                return RedirectToAction(nameof(PurchaseOrderList));
            }

            // Check if purchase order has any invoices or items
            bool hasInvoices = await _context.PurchaseInvoices.AnyAsync(pi => pi.PurchaseOrderId == id);
            bool hasItems = await _context.PurchaseOrderItems.AnyAsync(poi => poi.PurchaseOrderId == id);

            if (hasInvoices || hasItems)
            {
                TempData["ErrorMessage"] = "این سفارش خرید دارای فاکتور یا آیتم است و قابل حذف نیست";
                return RedirectToAction(nameof(PurchaseOrderList));
            }

            try
            {
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

        #region Purchase Order Items Methods

        // GET: PurchaseOrder/GetItem/{id} - For Edit Modal
        [HttpGet]
        public async Task<IActionResult> GetItem(Guid id)
        {
            var item = await _context.PurchaseOrderItems
                .Include(poi => poi.Material)
                .Include(poi => poi.Unit)
                .FirstOrDefaultAsync(poi => poi.PoItemId == id);

            if (item == null)
            {
                return NotFound();
            }

            return Json(new
            {
                poItemId = item.PoItemId,
                materialId = item.MaterialId,
                materialName = item.Material.MaterialName,
                quantity = item.Quantity,
                unitId = item.UnitId,
                unitName = item.Unit.UnitName,
                unitPrice = item.UnitPrice,
                totalPrice = item.TotalPrice,
                receivedQuantity = item.ReceivedQuantity,
                notes = item.Notes
            });
        }

        // GET: PurchaseOrder/GetMaterials - For Add/Edit Modal
        [HttpGet]
        public async Task<IActionResult> GetMaterials()
        {
            var materials = await _context.RawMaterials
                .Where(m => m.IsActive == true)
                .OrderBy(m => m.MaterialName)
                .Select(m => new { value = m.MaterialId, text = m.MaterialName })
                .ToListAsync();

            return Json(materials);
        }

        // GET: PurchaseOrder/GetUnits - For Add/Edit Modal
        [HttpGet]
        public async Task<IActionResult> GetUnits()
        {
            var units = await _context.Units
                .Where(u => u.IsActive == true)
                .OrderBy(u => u.UnitName)
                .Select(u => new { value = u.UnitId, text = u.UnitName })
                .ToListAsync();

            return Json(units);
        }

        // POST: PurchaseOrder/AddItem
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddItem(PurchaseOrderItem item)
        {
            try
            {
                // Check if purchase order exists
                var purchaseOrderExists = await _context.PurchaseOrders
                    .AnyAsync(po => po.PurchaseOrderId == item.PurchaseOrderId);

                if (!purchaseOrderExists)
                {
                    return Json(new { success = false, message = "سفارش خرید مورد نظر یافت نشد" });
                }

                // Check if material already exists in this purchase order
                var existingItem = await _context.PurchaseOrderItems
                    .AnyAsync(poi => poi.PurchaseOrderId == item.PurchaseOrderId &&
                                   poi.MaterialId == item.MaterialId);

                if (existingItem)
                {
                    return Json(new { success = false, message = "این ماده اولیه قبلاً به سفارش خرید اضافه شده است" });
                }

                item.PoItemId = Guid.NewGuid();
                item.TotalPrice = item.Quantity * item.UnitPrice;
                item.ReceivedQuantity = 0;

                _context.PurchaseOrderItems.Add(item);
                await _context.SaveChangesAsync();

                // Update purchase order total amount
                await UpdatePurchaseOrderTotalAsync(item.PurchaseOrderId);

                return Json(new { success = true, message = "آیتم جدید با موفقیت اضافه شد" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "خطا در افزودن آیتم: " + ex.Message });
            }
        }

        // POST: PurchaseOrder/EditItem
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditItem(PurchaseOrderItem item)
        {
            try
            {
                var existingItem = await _context.PurchaseOrderItems
                    .FirstOrDefaultAsync(poi => poi.PoItemId == item.PoItemId);

                if (existingItem == null)
                {
                    return Json(new { success = false, message = "آیتم مورد نظر یافت نشد" });
                }

                // Check if material already exists in this purchase order (excluding current item)
                var duplicateItem = await _context.PurchaseOrderItems
                    .AnyAsync(poi => poi.PurchaseOrderId == existingItem.PurchaseOrderId &&
                                   poi.MaterialId == item.MaterialId &&
                                   poi.PoItemId != item.PoItemId);

                if (duplicateItem)
                {
                    return Json(new { success = false, message = "این ماده اولیه قبلاً به سفارش خرید اضافه شده است" });
                }

                existingItem.MaterialId = item.MaterialId;
                existingItem.Quantity = item.Quantity;
                existingItem.UnitId = item.UnitId;
                existingItem.UnitPrice = item.UnitPrice;
                existingItem.TotalPrice = item.Quantity * item.UnitPrice;
                existingItem.Notes = item.Notes;

                await _context.SaveChangesAsync();

                // Update purchase order total amount
                await UpdatePurchaseOrderTotalAsync(existingItem.PurchaseOrderId);

                return Json(new { success = true, message = "آیتم با موفقیت ویرایش شد" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "خطا در ویرایش آیتم: " + ex.Message });
            }
        }

        // POST: PurchaseOrder/DeleteItem
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteItem(Guid id)
        {
            try
            {
                var item = await _context.PurchaseOrderItems.FindAsync(id);
                if (item == null)
                {
                    return Json(new { success = false, message = "آیتم مورد نظر یافت نشد" });
                }

                var purchaseOrderId = item.PurchaseOrderId;
                _context.PurchaseOrderItems.Remove(item);
                await _context.SaveChangesAsync();

                // Update purchase order total amount
                await UpdatePurchaseOrderTotalAsync(purchaseOrderId);

                return Json(new { success = true, message = "آیتم با موفقیت حذف شد" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "خطا در حذف آیتم: " + ex.Message });
            }
        }

        #endregion

        #region Private Methods

        private bool PurchaseOrderExists(Guid id)
        {
            return _context.PurchaseOrders.Any(e => e.PurchaseOrderId == id);
        }

        private async Task LoadSuppliers()
        {
            var suppliers = await _context.Suppliers
                .Where(s => s.IsActive == true)
                .OrderBy(s => s.SupplierName)
                .ToListAsync();

            ViewBag.Suppliers = suppliers.Select(s => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
            {
                Value = s.SupplierId.ToString(),
                Text = s.SupplierName
            }).ToList();
        }

        private async Task UpdatePurchaseOrderTotalAsync(Guid purchaseOrderId)
        {
            var totalAmount = await _context.PurchaseOrderItems
                .Where(poi => poi.PurchaseOrderId == purchaseOrderId)
                .SumAsync(poi => poi.TotalPrice);

            var purchaseOrder = await _context.PurchaseOrders.FindAsync(purchaseOrderId);
            if (purchaseOrder != null)
            {
                purchaseOrder.TotalAmount = totalAmount;
                await _context.SaveChangesAsync();
            }
        }

        #endregion
    }
}