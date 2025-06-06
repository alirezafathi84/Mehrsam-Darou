using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mehrsam_Darou.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using static Mehrsam_Darou.Helper.Helper;

namespace Mehrsam_Darou.Controllers
{
    public class CustomerController : BaseController
    {
        private readonly DarouAppContext _context;

        public CustomerController(DarouAppContext context) : base(context)
        {
            _context = context;
        }

        // GET: Customer/CustomerList
        public async Task<IActionResult> CustomerList(int? page, string searchKey)
        {
            var setting = await ReadSettingAsync(_context);
            int pageSize = Convert.ToInt32(setting.NumberPerPage ?? 10);
            int pageNumber = page ?? 1;

            IQueryable<Customer> query = _context.Customers;

            if (!string.IsNullOrWhiteSpace(searchKey))
            {
                query = query.Where(c => c.CustomerName.Contains(searchKey) ||
                                     c.CustomerCode.Contains(searchKey) ||
                                     c.ContactPerson.Contains(searchKey) ||
                                     c.Email.Contains(searchKey))
                            .OrderBy(c => c.CustomerName);
            }

            int total = await query.CountAsync();
            var items = await query
                .OrderBy(c => c.CustomerName)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var paginatedList = new PaginatedList<Customer>(items, total, pageNumber, pageSize);

            return View(paginatedList);
        }

        // GET: Customer/AddCustomer
        public IActionResult AddCustomer()
        {
            return View(new Customer { IsActive = true, CreatedDate = DateTime.Now });
        }

        // POST: Customer/AddCustomer
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCustomer(Customer customer)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    if (await _context.Customers.AnyAsync(c => c.CustomerCode == customer.CustomerCode))
                    {
                        TempData["ErrorMessage"] = "مشتری با این کد قبلاً ثبت شده است";
                        return View(customer);
                    }

                    customer.CustomerId = Guid.NewGuid();
                    customer.CreatedDate = DateTime.Now;

                    _context.Add(customer);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "مشتری جدید با موفقیت ایجاد شد";
                    return RedirectToAction(nameof(CustomerList));
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "خطا در ایجاد مشتری: " + ex.Message;
                }
            }

            return View(customer);
        }

        // GET: Customer/EditCustomer/5
        public async Task<IActionResult> EditCustomer(Guid id)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null)
            {
                return NotFound();
            }

            return View(customer);
        }

        // POST: Customer/EditCustomer/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCustomer(Guid id, Customer customer)
        {
            if (id != customer.CustomerId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (await _context.Customers.AnyAsync(c =>
                        c.CustomerId != id &&
                        c.CustomerCode == customer.CustomerCode))
                    {
                        TempData["ErrorMessage"] = "مشتری با این کد قبلاً ثبت شده است";
                        return View(customer);
                    }

                    var existingCustomer = await _context.Customers.FindAsync(id);
                    if (existingCustomer == null)
                    {
                        return NotFound();
                    }

                    // Keep the original creation date
                    customer.CreatedDate = existingCustomer.CreatedDate;

                    _context.Entry(existingCustomer).CurrentValues.SetValues(customer);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "اطلاعات مشتری با موفقیت به‌روزرسانی شد";
                    return RedirectToAction(nameof(CustomerList));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CustomerExists(customer.CustomerId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            return View(customer);
        }

        // POST: Customer/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null)
            {
                TempData["ErrorMessage"] = "مشتری مورد نظر یافت نشد";
                return RedirectToAction(nameof(CustomerList));
            }

            // Check if customer has any sales orders or invoices
            bool hasOrders = await _context.SalesOrders.AnyAsync(s => s.CustomerId == id);
            bool hasInvoices = await _context.SalesInvoices.AnyAsync(s => s.CustomerId == id);
            bool hasShipments = await _context.Shipments.AnyAsync(s => s.CustomerId == id);

            if (hasOrders || hasInvoices || hasShipments)
            {
                TempData["ErrorMessage"] = "این مشتری دارای سفارش، فاکتور یا حمل و نقل است و قابل حذف نیست";
                return RedirectToAction(nameof(CustomerList));
            }

            try
            {
                _context.Customers.Remove(customer);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "مشتری با موفقیت حذف شد";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "خطا در حذف مشتری: " + ex.Message;
            }

            return RedirectToAction(nameof(CustomerList));
        }

        private bool CustomerExists(Guid id)
        {
            return _context.Customers.Any(e => e.CustomerId == id);
        }
    }
}