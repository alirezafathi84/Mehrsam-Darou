// =============================================
// Fixed Payment Controller
// =============================================
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mehrsam_Darou.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using static Mehrsam_Darou.Helper.Helper;

namespace Mehrsam_Darou.Controllers
{
    public class PaymentController : BaseController
    {
        private readonly DarouAppContext _context;

        public PaymentController(DarouAppContext context) : base(context)
        {
            _context = context;
        }

        // GET: Payment/PaymentList
        public async Task<IActionResult> PaymentList(int? page, string searchKey, string transactionType, string status)
        {
            var setting = await ReadSettingAsync(_context);
            int pageSize = Convert.ToInt32(setting.NumberPerPage ?? 10);
            int pageNumber = page ?? 1;

            IQueryable<PaymentTransaction> query = _context.PaymentTransactions
                .Include(p => p.PaymentMethod)
                .Include(p => p.Customer)
                .Include(p => p.Supplier);

            if (!string.IsNullOrWhiteSpace(searchKey))
            {
                query = query.Where(p => p.TransactionNumber.Contains(searchKey) ||
                                     p.ReferenceNumber.Contains(searchKey) ||
                                     (p.Customer != null && p.Customer.CustomerName.Contains(searchKey)) ||
                                     (p.Supplier != null && p.Supplier.SupplierName.Contains(searchKey)))
                            .OrderByDescending(p => p.CreatedDate);
            }

            if (!string.IsNullOrWhiteSpace(transactionType))
            {
                query = query.Where(p => p.TransactionType == transactionType);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(p => p.Status == status);
            }

            int total = await query.CountAsync();
            var items = await query
                .OrderByDescending(p => p.CreatedDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var paginatedList = new PaginatedList<PaymentTransaction>(items, total, pageNumber, pageSize);

            ViewBag.TransactionTypes = new List<string> { "دریافت", "پرداخت" };
            ViewBag.StatusList = new List<string> { "در انتظار", "تایید شده", "برگشت خورده", "لغو شده" };

            return View(paginatedList);
        }

        // GET: Payment/AddPayment
        public async Task<IActionResult> AddPayment()
        {
            await LoadPaymentViewBags();

            return View(new PaymentTransaction
            {
                TransactionDate = DateOnly.FromDateTime(DateTime.Now.Date),
                Status = "در انتظار",
                Currency = "IRR",
                CreatedDate = DateTime.Now
            });
        }

        // POST: Payment/AddPayment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddPayment(PaymentTransaction paymentTransaction)
        {
            // Remove PaymentMethod from ModelState to avoid validation issues
            ModelState.Remove("PaymentMethod");
            ModelState.Remove("Customer");
            ModelState.Remove("Supplier");

            if (ModelState.IsValid)
            {
                try
                {
                    if (await _context.PaymentTransactions.AnyAsync(p => p.TransactionNumber == paymentTransaction.TransactionNumber))
                    {
                        TempData["ErrorMessage"] = "تراکنش با این شماره قبلاً ثبت شده است";
                        await LoadPaymentViewBags();
                        return View(paymentTransaction);
                    }

                    paymentTransaction.TransactionId = Guid.NewGuid();
                    paymentTransaction.CreatedDate = DateTime.Now;

                    // Ensure navigation properties are null to avoid EF issues
                    paymentTransaction.PaymentMethod = null;
                    paymentTransaction.Customer = null;
                    paymentTransaction.Supplier = null;

                    _context.Add(paymentTransaction);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "تراکنش پرداخت جدید با موفقیت ایجاد شد";
                    return RedirectToAction(nameof(PaymentList));
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "خطا در ایجاد تراکنش پرداخت: " + ex.Message;
                }
            }

            await LoadPaymentViewBags();
            return View(paymentTransaction);
        }

        // GET: Payment/EditPayment/5
        public async Task<IActionResult> EditPayment(Guid id)
        {
            var paymentTransaction = await _context.PaymentTransactions.FindAsync(id);
            if (paymentTransaction == null)
            {
                return NotFound();
            }

            await LoadPaymentViewBags();
            ViewBag.StatusList = new List<string> { "در انتظار", "تایید شده", "برگشت خورده", "لغو شده" };

            return View(paymentTransaction);
        }

        // POST: Payment/EditPayment/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPayment(Guid id, PaymentTransaction paymentTransaction)
        {
            if (id != paymentTransaction.TransactionId)
            {
                return NotFound();
            }

            // Remove navigation properties from ModelState
            ModelState.Remove("PaymentMethod");
            ModelState.Remove("Customer");
            ModelState.Remove("Supplier");

            if (ModelState.IsValid)
            {
                try
                {
                    var existingTransaction = await _context.PaymentTransactions.FindAsync(id);
                    if (existingTransaction == null)
                    {
                        return NotFound();
                    }

                    // Update only the necessary fields
                    existingTransaction.TransactionType = paymentTransaction.TransactionType;
                    existingTransaction.Amount = paymentTransaction.Amount;
                    existingTransaction.PaymentMethodId = paymentTransaction.PaymentMethodId;
                    existingTransaction.CustomerId = paymentTransaction.CustomerId;
                    existingTransaction.SupplierId = paymentTransaction.SupplierId;
                    existingTransaction.TransactionDate = paymentTransaction.TransactionDate;
                    existingTransaction.DueDate = paymentTransaction.DueDate;
                    existingTransaction.ReferenceNumber = paymentTransaction.ReferenceNumber;
                    existingTransaction.BankName = paymentTransaction.BankName;
                    existingTransaction.AccountNumber = paymentTransaction.AccountNumber;
                    existingTransaction.Status = paymentTransaction.Status;
                    existingTransaction.Description = paymentTransaction.Description;

                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "اطلاعات تراکنش پرداخت با موفقیت به‌روزرسانی شد";
                    return RedirectToAction(nameof(PaymentList));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PaymentTransactionExists(paymentTransaction.TransactionId))
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
                    TempData["ErrorMessage"] = "خطا در به‌روزرسانی تراکنش پرداخت: " + ex.Message;
                }
            }

            await LoadPaymentViewBags();
            ViewBag.StatusList = new List<string> { "در انتظار", "تایید شده", "برگشت خورده", "لغو شده" };

            return View(paymentTransaction);
        }

        // POST: Payment/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var paymentTransaction = await _context.PaymentTransactions.FindAsync(id);
            if (paymentTransaction == null)
            {
                TempData["ErrorMessage"] = "تراکنش پرداخت مورد نظر یافت نشد";
                return RedirectToAction(nameof(PaymentList));
            }

            try
            {
                _context.PaymentTransactions.Remove(paymentTransaction);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "تراکنش پرداخت با موفقیت حذف شد";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "خطا در حذف تراکنش پرداخت: " + ex.Message;
            }

            return RedirectToAction(nameof(PaymentList));
        }

        private async Task LoadPaymentViewBags()
        {
            // Load as anonymous objects to avoid binding issues
            ViewBag.PaymentMethods = await _context.PaymentMethods
                .Where(p => p.IsActive == true)
                .Select(p => new { p.PaymentMethodId, p.MethodName, p.MethodType })
                .ToListAsync();

            ViewBag.Customers = await _context.Customers
                .Where(c => c.IsActive == true)
                .Select(c => new { c.CustomerId, c.CustomerName, c.CustomerCode })
                .ToListAsync();

            ViewBag.Suppliers = await _context.Suppliers
                .Where(s => s.IsActive == true)
                .Select(s => new { s.SupplierId, s.SupplierName, s.SupplierCode })
                .ToListAsync();

            ViewBag.TransactionTypes = new List<string> { "دریافت", "پرداخت" };
            ViewBag.InvoiceTypes = new List<string> { "فروش", "خرید" };
        }

        private bool PaymentTransactionExists(Guid id)
        {
            return _context.PaymentTransactions.Any(e => e.TransactionId == id);
        }
    }
}