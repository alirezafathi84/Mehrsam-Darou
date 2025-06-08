using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mehrsam_Darou.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using static Mehrsam_Darou.Helper.Helper;

namespace Mehrsam_Darou.Controllers
{
    public class PurchaseInvoiceController : BaseController
    {
        private readonly DarouAppContext _context;

        public PurchaseInvoiceController(DarouAppContext context) : base(context)
        {
            _context = context;
        }

        // GET: PurchaseInvoice/PurchaseInvoiceList
        public async Task<IActionResult> PurchaseInvoiceList(int? page, string searchKey)
        {
            var setting = await ReadSettingAsync(_context);
            int pageSize = Convert.ToInt32(setting.NumberPerPage ?? 10);
            int pageNumber = page ?? 1;

            IQueryable<PurchaseInvoice> query = _context.PurchaseInvoices
                .Include(p => p.Supplier)
                .Include(p => p.PurchaseOrder);

            if (!string.IsNullOrWhiteSpace(searchKey))
            {
                query = query.Where(p => p.InvoiceNumber.Contains(searchKey) ||
                                     p.SupplierInvoiceNumber.Contains(searchKey) ||
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

            var paginatedList = new PaginatedList<PurchaseInvoice>(items, total, pageNumber, pageSize);

            return View(paginatedList);
        }

        // GET: PurchaseInvoice/AddPurchaseInvoice
        public async Task<IActionResult> AddPurchaseInvoice()
        {
            await LoadDropdownData();

            return View(new PurchaseInvoice
            {
                InvoiceDate = DateOnly.FromDateTime(DateTime.Now),
                Status = "پیش‌نویس",
                Currency = "IRR",
                CreatedDate = DateTime.Now,
                TaxAmount = 0,
                DiscountAmount = 0,
                PaidAmount = 0
            });
        }

        // POST: PurchaseInvoice/AddPurchaseInvoice
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddPurchaseInvoice(PurchaseInvoice purchaseInvoice)
        {
            // Remove navigation properties from ModelState validation
            ModelState.Remove("Supplier");
            ModelState.Remove("PurchaseOrder");

            if (ModelState.IsValid)
            {
                try
                {
                    if (await _context.PurchaseInvoices.AnyAsync(p => p.InvoiceNumber == purchaseInvoice.InvoiceNumber))
                    {
                        TempData["ErrorMessage"] = "فاکتور خرید با این شماره قبلاً ثبت شده است";
                        await LoadDropdownData();
                        return View(purchaseInvoice);
                    }

                    purchaseInvoice.PurchaseInvoiceId = Guid.NewGuid();
                    purchaseInvoice.CreatedDate = DateTime.Now;

                    _context.Add(purchaseInvoice);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "فاکتور خرید جدید با موفقیت ایجاد شد";
                    return RedirectToAction(nameof(PurchaseInvoiceList));
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "خطا در ایجاد فاکتور خرید: " + ex.Message;
                }
            }

            await LoadDropdownData();
            return View(purchaseInvoice);
        }

        // GET: PurchaseInvoice/EditPurchaseInvoice/5
        public async Task<IActionResult> EditPurchaseInvoice(Guid id)
        {
            var purchaseInvoice = await _context.PurchaseInvoices
                .Include(p => p.Supplier)
                .Include(p => p.PurchaseOrder)
                .FirstOrDefaultAsync(p => p.PurchaseInvoiceId == id);

            if (purchaseInvoice == null)
            {
                return NotFound();
            }

            await LoadDropdownData();
            return View(purchaseInvoice);
        }

        // POST: PurchaseInvoice/EditPurchaseInvoice/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPurchaseInvoice(Guid id, PurchaseInvoice purchaseInvoice)
        {
            if (id != purchaseInvoice.PurchaseInvoiceId)
            {
                return NotFound();
            }

            // Remove navigation properties from ModelState validation
            ModelState.Remove("Supplier");
            ModelState.Remove("PurchaseOrder");

            if (ModelState.IsValid)
            {
                try
                {
                    if (await _context.PurchaseInvoices.AnyAsync(p =>
                        p.PurchaseInvoiceId != id &&
                        p.InvoiceNumber == purchaseInvoice.InvoiceNumber))
                    {
                        TempData["ErrorMessage"] = "فاکتور خرید با این شماره قبلاً ثبت شده است";
                        await LoadDropdownData();
                        return View(purchaseInvoice);
                    }

                    var existingPurchaseInvoice = await _context.PurchaseInvoices.FindAsync(id);
                    if (existingPurchaseInvoice == null)
                    {
                        return NotFound();
                    }

                    // Keep the original creation date
                    purchaseInvoice.CreatedDate = existingPurchaseInvoice.CreatedDate;

                    _context.Entry(existingPurchaseInvoice).CurrentValues.SetValues(purchaseInvoice);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "اطلاعات فاکتور خرید با موفقیت به‌روزرسانی شد";
                    return RedirectToAction(nameof(PurchaseInvoiceList));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PurchaseInvoiceExists(purchaseInvoice.PurchaseInvoiceId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            await LoadDropdownData();
            return View(purchaseInvoice);
        }

        // POST: PurchaseInvoice/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var purchaseInvoice = await _context.PurchaseInvoices.FindAsync(id);
            if (purchaseInvoice == null)
            {
                TempData["ErrorMessage"] = "فاکتور خرید مورد نظر یافت نشد";
                return RedirectToAction(nameof(PurchaseInvoiceList));
            }

            // Check if purchase invoice has any items
            bool hasItems = await _context.PurchaseInvoiceItems.AnyAsync(pii => pii.PurchaseInvoiceId == id);

            if (hasItems)
            {
                TempData["ErrorMessage"] = "این فاکتور خرید دارای آیتم است و قابل حذف نیست";
                return RedirectToAction(nameof(PurchaseInvoiceList));
            }

            try
            {
                _context.PurchaseInvoices.Remove(purchaseInvoice);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "فاکتور خرید با موفقیت حذف شد";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "خطا در حذف فاکتور خرید: " + ex.Message;
            }

            return RedirectToAction(nameof(PurchaseInvoiceList));
        }

        private bool PurchaseInvoiceExists(Guid id)
        {
            return _context.PurchaseInvoices.Any(e => e.PurchaseInvoiceId == id);
        }

        private async Task LoadDropdownData()
        {
            ViewBag.Suppliers = await _context.Suppliers
                .Where(s => s.IsActive == true)
                .OrderBy(s => s.SupplierName)
                .ToListAsync();

            ViewBag.PurchaseOrders = await _context.PurchaseOrders
                .Include(po => po.Supplier)
                .Where(po => po.Status != "لغو شده")
                .OrderByDescending(po => po.CreatedDate)
                .ToListAsync();
        }
    }
}