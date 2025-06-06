using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mehrsam_Darou.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using static Mehrsam_Darou.Helper.Helper;

namespace Mehrsam_Darou.Controllers
{
    public class SupplierController : BaseController
    {
        private readonly DarouAppContext _context;

        public SupplierController(DarouAppContext context) : base(context)
        {
            _context = context;
        }

        // GET: Supplier/SupplierList
        public async Task<IActionResult> SupplierList(int? page, string searchKey)
        {
            var setting = await ReadSettingAsync(_context);
            int pageSize = Convert.ToInt32(setting.NumberPerPage ?? 10);
            int pageNumber = page ?? 1;

            IQueryable<Supplier> query = _context.Suppliers;

            if (!string.IsNullOrWhiteSpace(searchKey))
            {
                query = query.Where(s => s.SupplierName.Contains(searchKey) ||
                                     s.SupplierCode.Contains(searchKey) ||
                                     s.ContactPerson.Contains(searchKey) ||
                                     s.Email.Contains(searchKey))
                            .OrderBy(s => s.SupplierName);
            }

            int total = await query.CountAsync();
            var items = await query
                .OrderBy(s => s.SupplierName)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var paginatedList = new PaginatedList<Supplier>(items, total, pageNumber, pageSize);

            return View(paginatedList);
        }

        // GET: Supplier/AddSupplier
        public IActionResult AddSupplier()
        {
            return View(new Supplier { IsActive = true, CreatedDate = DateTime.Now });
        }

        // POST: Supplier/AddSupplier
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddSupplier(Supplier supplier)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    if (await _context.Suppliers.AnyAsync(s => s.SupplierCode == supplier.SupplierCode))
                    {
                        TempData["ErrorMessage"] = "تأمین‌کننده با این کد قبلاً ثبت شده است";
                        return View(supplier);
                    }

                    supplier.SupplierId = Guid.NewGuid();
                    supplier.CreatedDate = DateTime.Now;

                    _context.Add(supplier);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "تأمین‌کننده جدید با موفقیت ایجاد شد";
                    return RedirectToAction(nameof(SupplierList));
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "خطا در ایجاد تأمین‌کننده: " + ex.Message;
                }
            }

            return View(supplier);
        }

        // GET: Supplier/EditSupplier/5
        public async Task<IActionResult> EditSupplier(Guid id)
        {
            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier == null)
            {
                return NotFound();
            }

            return View(supplier);
        }

        // POST: Supplier/EditSupplier/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSupplier(Guid id, Supplier supplier)
        {
            if (id != supplier.SupplierId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (await _context.Suppliers.AnyAsync(s =>
                        s.SupplierId != id &&
                        s.SupplierCode == supplier.SupplierCode))
                    {
                        TempData["ErrorMessage"] = "تأمین‌کننده با این کد قبلاً ثبت شده است";
                        return View(supplier);
                    }

                    var existingSupplier = await _context.Suppliers.FindAsync(id);
                    if (existingSupplier == null)
                    {
                        return NotFound();
                    }

                    // Keep the original creation date
                    supplier.CreatedDate = existingSupplier.CreatedDate;

                    _context.Entry(existingSupplier).CurrentValues.SetValues(supplier);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "اطلاعات تأمین‌کننده با موفقیت به‌روزرسانی شد";
                    return RedirectToAction(nameof(SupplierList));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SupplierExists(supplier.SupplierId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            return View(supplier);
        }

        // POST: Supplier/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier == null)
            {
                TempData["ErrorMessage"] = "تأمین‌کننده مورد نظر یافت نشد";
                return RedirectToAction(nameof(SupplierList));
            }

            // Check if supplier has any purchase orders or invoices
            bool hasOrders = await _context.PurchaseOrders.AnyAsync(p => p.SupplierId == id);
            bool hasInvoices = await _context.PurchaseInvoices.AnyAsync(p => p.SupplierId == id);

            if (hasOrders || hasInvoices)
            {
                TempData["ErrorMessage"] = "این تأمین‌کننده دارای سفارش خرید یا فاکتور خرید است و قابل حذف نیست";
                return RedirectToAction(nameof(SupplierList));
            }

            try
            {
                _context.Suppliers.Remove(supplier);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "تأمین‌کننده با موفقیت حذف شد";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "خطا در حذف تأمین‌کننده: " + ex.Message;
            }

            return RedirectToAction(nameof(SupplierList));
        }

        // GET: Supplier/GetSupplierDetails/5
        public async Task<IActionResult> GetSupplierDetails(Guid id)
        {
            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier == null)
            {
                return NotFound();
            }

            // Get supplier statistics
            var totalOrders = await _context.PurchaseOrders.CountAsync(p => p.SupplierId == id);
            var totalInvoices = await _context.PurchaseInvoices.CountAsync(p => p.SupplierId == id);
            var totalAmount = await _context.PurchaseInvoices
                .Where(p => p.SupplierId == id)
                .SumAsync(p => (decimal?)p.TotalAmount) ?? 0;

            return Json(new
            {
                supplier = supplier,
                statistics = new
                {
                    totalOrders = totalOrders,
                    totalInvoices = totalInvoices,
                    totalAmount = totalAmount
                }
            });
        }

        private bool SupplierExists(Guid id)
        {
            return _context.Suppliers.Any(e => e.SupplierId == id);
        }
    }
}