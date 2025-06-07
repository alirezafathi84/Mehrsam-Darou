using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mehrsam_Darou.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using static Mehrsam_Darou.Helper.Helper;

namespace Mehrsam_Darou.Controllers
{
    public class SalesInvoiceController : BaseController
    {
        private readonly DarouAppContext _context;

        public SalesInvoiceController(DarouAppContext context) : base(context)
        {
            _context = context;
        }

        // GET: SalesInvoice/SalesInvoiceList
        public async Task<IActionResult> SalesInvoiceList(int? page, string searchKey, string status)
        {
            var setting = await ReadSettingAsync(_context);
            int pageSize = Convert.ToInt32(setting.NumberPerPage ?? 10);
            int pageNumber = page ?? 1;

            IQueryable<SalesInvoice> query = _context.SalesInvoices
                .Include(s => s.Customer)
                .Include(s => s.SalesOrder);

            if (!string.IsNullOrWhiteSpace(searchKey))
            {
                query = query.Where(s => s.InvoiceNumber.Contains(searchKey) ||
                                     s.Customer.CustomerName.Contains(searchKey) ||
                                     s.Customer.CustomerCode.Contains(searchKey));
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

            var paginatedList = new PaginatedList<SalesInvoice>(items, total, pageNumber, pageSize);

            ViewBag.StatusFilter = status;
            return View(paginatedList);
        }

        // GET: SalesInvoice/AddSalesInvoice
        public async Task<IActionResult> AddSalesInvoice()
        {
            await PopulateDropdowns();

            var invoice = new SalesInvoice
            {
                InvoiceDate = DateOnly.FromDateTime(DateTime.Now),
                DueDate = DateOnly.FromDateTime(DateTime.Now.AddDays(30)),
                CreatedDate = DateTime.Now,
                Status = "پیش‌نویس",
                Currency = "IRR"
            };

            return View(invoice);
        }

        // POST: SalesInvoice/AddSalesInvoice
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddSalesInvoice(SalesInvoice invoice)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    if (await _context.SalesInvoices.AnyAsync(s => s.InvoiceNumber == invoice.InvoiceNumber))
                    {
                        TempData["ErrorMessage"] = "فاکتور با این شماره قبلاً ثبت شده است";
                        await PopulateDropdowns();
                        return View(invoice);
                    }

                    invoice.SalesInvoiceId = Guid.NewGuid();
                    invoice.CreatedDate = DateTime.Now;

                    // Calculate totals if not provided
                    if (invoice.TotalAmount == 0)
                    {
                        invoice.TotalAmount = invoice.Subtotal + (invoice.TaxAmount ?? 0) - (invoice.DiscountAmount ?? 0);
                    }

                    _context.Add(invoice);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "فاکتور فروش جدید با موفقیت ایجاد شد";
                    return RedirectToAction(nameof(SalesInvoiceList));
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "خطا در ایجاد فاکتور: " + ex.Message;
                }
            }

            await PopulateDropdowns();
            return View(invoice);
        }

        // GET: SalesInvoice/EditSalesInvoice/5
        public async Task<IActionResult> EditSalesInvoice(Guid id)
        {
            var invoice = await _context.SalesInvoices
                .Include(s => s.Customer)
                .Include(s => s.SalesOrder)
                .FirstOrDefaultAsync(s => s.SalesInvoiceId == id);

            if (invoice == null)
            {
                return NotFound();
            }

            await PopulateDropdowns();
            return View(invoice);
        }

        // POST: SalesInvoice/EditSalesInvoice/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSalesInvoice(Guid id, SalesInvoice invoice)
        {
            if (id != invoice.SalesInvoiceId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (await _context.SalesInvoices.AnyAsync(s =>
                        s.SalesInvoiceId != id &&
                        s.InvoiceNumber == invoice.InvoiceNumber))
                    {
                        TempData["ErrorMessage"] = "فاکتور با این شماره قبلاً ثبت شده است";
                        await PopulateDropdowns();
                        return View(invoice);
                    }

                    var existingInvoice = await _context.SalesInvoices.FindAsync(id);
                    if (existingInvoice == null)
                    {
                        return NotFound();
                    }

                    // Keep the original creation date
                    invoice.CreatedDate = existingInvoice.CreatedDate;

                    // Calculate totals if not provided
                    if (invoice.TotalAmount == 0)
                    {
                        invoice.TotalAmount = invoice.Subtotal + (invoice.TaxAmount ?? 0) - (invoice.DiscountAmount ?? 0);
                    }

                    _context.Entry(existingInvoice).CurrentValues.SetValues(invoice);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "فاکتور فروش با موفقیت به‌روزرسانی شد";
                    return RedirectToAction(nameof(SalesInvoiceList));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SalesInvoiceExists(invoice.SalesInvoiceId))
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
            return View(invoice);
        }

        // POST: SalesInvoice/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var invoice = await _context.SalesInvoices.FindAsync(id);
            if (invoice == null)
            {
                TempData["ErrorMessage"] = "فاکتور مورد نظر یافت نشد";
                return RedirectToAction(nameof(SalesInvoiceList));
            }

            // Check if invoice has items
            bool hasItems = await _context.SalesInvoiceItems.AnyAsync(s => s.SalesInvoiceId == id);

            if (hasItems)
            {
                TempData["ErrorMessage"] = "این فاکتور دارای آیتم است و قابل حذف نیست";
                return RedirectToAction(nameof(SalesInvoiceList));
            }

            // Only allow deletion of draft invoices
            if (invoice.Status != "پیش‌نویس")
            {
                TempData["ErrorMessage"] = "تنها فاکتورهای پیش‌نویس قابل حذف هستند";
                return RedirectToAction(nameof(SalesInvoiceList));
            }

            try
            {
                _context.SalesInvoices.Remove(invoice);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "فاکتور با موفقیت حذف شد";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "خطا در حذف فاکتور: " + ex.Message;
            }

            return RedirectToAction(nameof(SalesInvoiceList));
        }

        private bool SalesInvoiceExists(Guid id)
        {
            return _context.SalesInvoices.Any(e => e.SalesInvoiceId == id);
        }

        private async Task PopulateDropdowns()
        {
            ViewBag.Customers = await _context.Customers
                .Where(c => c.IsActive == true)
                .OrderBy(c => c.CustomerName)
                .Select(c => new { c.CustomerId, c.CustomerName, c.CustomerCode })
                .ToListAsync();

            ViewBag.SalesOrders = await _context.SalesOrders
                .Where(s => s.Status != "لغو شده" && s.Status != "تحویل داده شده")
                .OrderByDescending(s => s.CreatedDate)
                .Select(s => new { s.SalesOrderId, s.SoNumber })
                .ToListAsync();
        }
    }
}