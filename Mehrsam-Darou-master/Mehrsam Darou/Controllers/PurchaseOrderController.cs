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
            ViewBag.Suppliers = await _context.Suppliers
                .Where(s => s.IsActive == true)
                .OrderBy(s => s.SupplierName)
                .ToListAsync();

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

        private bool PurchaseOrderExists(Guid id)
        {
            return _context.PurchaseOrders.Any(e => e.PurchaseOrderId == id);
        }

        private async Task LoadSuppliers()
        {
            ViewBag.Suppliers = await _context.Suppliers
                .Where(s => s.IsActive == true)
                .OrderBy(s => s.SupplierName)
                .ToListAsync();
        }
    }
}