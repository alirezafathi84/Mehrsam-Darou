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
            // Enhanced validation
            if (string.IsNullOrWhiteSpace(supplier.SupplierName))
            {
                ModelState.AddModelError(nameof(supplier.SupplierName), "نام تأمین‌کننده الزامی است");
            }

            if (string.IsNullOrWhiteSpace(supplier.SupplierCode))
            {
                ModelState.AddModelError(nameof(supplier.SupplierCode), "کد تأمین‌کننده الزامی است");
            }

            // Validate email format if provided
            if (!string.IsNullOrWhiteSpace(supplier.Email) && !IsValidEmail(supplier.Email))
            {
                ModelState.AddModelError(nameof(supplier.Email), "فرمت ایمیل صحیح نیست");
            }

            // Validate lead time range
            if (supplier.LeadTimeDays.HasValue && (supplier.LeadTimeDays < 0 || supplier.LeadTimeDays > 365))
            {
                ModelState.AddModelError(nameof(supplier.LeadTimeDays), "مدت تحویل باید بین 0 تا 365 روز باشد");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Check for duplicate supplier code
                    if (await _context.Suppliers.AnyAsync(s => s.SupplierCode == supplier.SupplierCode))
                    {
                        ModelState.AddModelError(nameof(supplier.SupplierCode), "تأمین‌کننده با این کد قبلاً ثبت شده است");
                        return View(supplier);
                    }

                    // Check for duplicate supplier name
                    if (await _context.Suppliers.AnyAsync(s => s.SupplierName.ToLower() == supplier.SupplierName.ToLower()))
                    {
                        ModelState.AddModelError(nameof(supplier.SupplierName), "تأمین‌کننده با این نام قبلاً ثبت شده است");
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
                    ModelState.AddModelError("", "خطا در ایجاد تأمین‌کننده: " + ex.Message);
                    TempData["ErrorMessage"] = "خطا در ایجاد تأمین‌کننده: " + ex.Message;
                }
            }

            // Log ModelState errors for debugging
            LogModelStateErrors();
            return View(supplier);
        }

        // GET: Supplier/EditSupplier/5
        public async Task<IActionResult> EditSupplier(Guid id)
        {
            if (id == Guid.Empty)
            {
                TempData["ErrorMessage"] = "معرف تأمین‌کننده نامعتبر است";
                return RedirectToAction(nameof(SupplierList));
            }

            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier == null)
            {
                TempData["ErrorMessage"] = "تأمین‌کننده مورد نظر یافت نشد";
                return RedirectToAction(nameof(SupplierList));
            }

            return View(supplier);
        }

        // POST: Supplier/EditSupplier/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSupplier(Guid id, Supplier supplier)
        {
            if (id == Guid.Empty)
            {
                TempData["ErrorMessage"] = "معرف تأمین‌کننده نامعتبر است";
                return RedirectToAction(nameof(SupplierList));
            }

            if (id != supplier.SupplierId)
            {
                ModelState.AddModelError("", "عدم تطابق شناسه تأمین‌کننده");
                return View(supplier);
            }

            // Enhanced validation
            if (string.IsNullOrWhiteSpace(supplier.SupplierName))
            {
                ModelState.AddModelError(nameof(supplier.SupplierName), "نام تأمین‌کننده الزامی است");
            }

            if (string.IsNullOrWhiteSpace(supplier.SupplierCode))
            {
                ModelState.AddModelError(nameof(supplier.SupplierCode), "کد تأمین‌کننده الزامی است");
            }

            // Validate email format if provided
            if (!string.IsNullOrWhiteSpace(supplier.Email) && !IsValidEmail(supplier.Email))
            {
                ModelState.AddModelError(nameof(supplier.Email), "فرمت ایمیل صحیح نیست");
            }

            // Validate lead time range
            if (supplier.LeadTimeDays.HasValue && (supplier.LeadTimeDays < 0 || supplier.LeadTimeDays > 365))
            {
                ModelState.AddModelError(nameof(supplier.LeadTimeDays), "مدت تحویل باید بین 0 تا 365 روز باشد");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Check for duplicate supplier code (excluding current supplier)
                    if (await _context.Suppliers.AnyAsync(s =>
                        s.SupplierId != id &&
                        s.SupplierCode == supplier.SupplierCode))
                    {
                        ModelState.AddModelError(nameof(supplier.SupplierCode), "تأمین‌کننده با این کد قبلاً ثبت شده است");
                        return View(supplier);
                    }

                    // Check for duplicate supplier name (excluding current supplier)
                    if (await _context.Suppliers.AnyAsync(s =>
                        s.SupplierId != id &&
                        s.SupplierName.ToLower() == supplier.SupplierName.ToLower()))
                    {
                        ModelState.AddModelError(nameof(supplier.SupplierName), "تأمین‌کننده با این نام قبلاً ثبت شده است");
                        return View(supplier);
                    }

                    var existingSupplier = await _context.Suppliers.FindAsync(id);
                    if (existingSupplier == null)
                    {
                        TempData["ErrorMessage"] = "تأمین‌کننده مورد نظر یافت نشد";
                        return RedirectToAction(nameof(SupplierList));
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
                        TempData["ErrorMessage"] = "تأمین‌کننده مورد نظر یافت نشد";
                        return RedirectToAction(nameof(SupplierList));
                    }
                    else
                    {
                        ModelState.AddModelError("", "خطای همزمانی در به‌روزرسانی اطلاعات");
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "خطا در به‌روزرسانی تأمین‌کننده: " + ex.Message);
                    TempData["ErrorMessage"] = "خطا در به‌روزرسانی تأمین‌کننده: " + ex.Message;
                }
            }

            // Log ModelState errors for debugging
            LogModelStateErrors();
            return View(supplier);
        }

        // POST: Supplier/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            if (id == Guid.Empty)
            {
                TempData["ErrorMessage"] = "معرف تأمین‌کننده نامعتبر است";
                return RedirectToAction(nameof(SupplierList));
            }

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



        /// <summary>
        /// دریافت لیست تمام تأمین‌کنندگان فعال
        /// </summary>
        /// <summary>
        /// دریافت لیست تمام تأمین‌کنندگان فعال
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetSuppliers()
        {
            try
            {
                var suppliers = await _context.Suppliers
                    .Where(s => s.IsActive == true)
                    .OrderBy(s => s.SupplierName)
                    .Select(s => new
                    {
                        supplierId = s.SupplierId.ToString(), // Convert Guid to string for JSON
                        s.SupplierName,
                        s.ContactPerson,
                        s.Phone,
                        s.Email
                    })
                    .ToListAsync();

                return Json(suppliers);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"خطا در دریافت لیست تأمین‌کنندگان: {ex.Message}" });
            }
        }












        // GET: Supplier/GetSupplierDetails/5
        public async Task<IActionResult> GetSupplierDetails(Guid id)
        {
            if (id == Guid.Empty)
            {
                return BadRequest("معرف تأمین‌کننده نامعتبر است");
            }

            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier == null)
            {
                return NotFound("تأمین‌کننده مورد نظر یافت نشد");
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

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private void LogModelStateErrors()
        {
            foreach (var modelState in ModelState.Values)
            {
                foreach (var error in modelState.Errors)
                {
                    System.Diagnostics.Debug.WriteLine($"ModelState Error: {error.ErrorMessage}");
                }
            }
        }
    }
}