// =============================================
// Accounting Controller
// =============================================
using Mehrsam_Darou.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Mehrsam_Darou.Helper.Helper;

namespace Mehrsam_Darou.Controllers
{
    public class AccountingController : BaseController
    {
        private readonly DarouAppContext _context;

        public AccountingController(DarouAppContext context) : base(context)
        {
            _context = context;
        }

        // GET: Accounting/AccountingReport
        public async Task<IActionResult> AccountingReport(int? page, string searchKey, string entryType, string status)
        {
            var setting = await ReadSettingAsync(_context);
            int pageSize = Convert.ToInt32(setting.NumberPerPage ?? 10);
            int pageNumber = page ?? 1;

            IQueryable<JournalEntry> query = _context.JournalEntries;

            if (!string.IsNullOrWhiteSpace(searchKey))
            {
                query = query.Where(j => j.EntryNumber.Contains(searchKey) ||
                                     j.Description.Contains(searchKey) ||
                                     j.ReferenceNumber.Contains(searchKey))
                            .OrderByDescending(j => j.CreatedDate);
            }

            if (!string.IsNullOrWhiteSpace(entryType))
            {
                query = query.Where(j => j.ReferenceType == entryType);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(j => j.Status == status);
            }

            int total = await query.CountAsync();
            var items = await query
                .OrderByDescending(j => j.CreatedDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var paginatedList = new PaginatedList<JournalEntry>(items, total, pageNumber, pageSize);

            ViewBag.EntryTypes = new List<string> { "فاکتور فروش", "فاکتور خرید", "پرداخت", "دریافت", "تسویه" };
            ViewBag.StatusList = new List<string> { "پیش‌نویس", "تایید شده", "لغو شده" };

            return View(paginatedList);
        }

        // GET: Accounting/AddJournalEntry
        public async Task<IActionResult> AddJournalEntry()
        {
            ViewBag.Accounts = await _context.ChartOfAccounts.Where(c => c.IsActive == true).OrderBy(c => c.AccountCode).ToListAsync();
            ViewBag.EntryTypes = new List<string> { "فاکتور فروش", "فاکتور خرید", "پرداخت", "دریافت", "تسویه" };

            return View(new JournalEntry
            {
                EntryDate = DateOnly.FromDateTime(DateTime.Now.Date),
                Status = "پیش‌نویس",
                CreatedDate = DateTime.Now
            });
        }

        // POST: Accounting/AddJournalEntry
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddJournalEntry(JournalEntry journalEntry)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    if (await _context.JournalEntries.AnyAsync(j => j.EntryNumber == journalEntry.EntryNumber))
                    {
                        TempData["ErrorMessage"] = "سند با این شماره قبلاً ثبت شده است";
                        ViewBag.Accounts = await _context.ChartOfAccounts.Where(c => c.IsActive == true).OrderBy(c => c.AccountCode).ToListAsync();
                        ViewBag.EntryTypes = new List<string> { "فاکتور فروش", "فاکتور خرید", "پرداخت", "دریافت", "تسویه" };
                        return View(journalEntry);
                    }

                    journalEntry.EntryId = Guid.NewGuid();
                    journalEntry.CreatedDate = DateTime.Now;

                    _context.Add(journalEntry);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "سند حسابداری جدید با موفقیت ایجاد شد";
                    return RedirectToAction(nameof(AccountingReport));
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "خطا در ایجاد سند حسابداری: " + ex.Message;
                }
            }

            ViewBag.Accounts = await _context.ChartOfAccounts.Where(c => c.IsActive == true).OrderBy(c => c.AccountCode).ToListAsync();
            ViewBag.EntryTypes = new List<string> { "فاکتور فروش", "فاکتور خرید", "پرداخت", "دریافت", "تسویه" };
            return View(journalEntry);
        }

        // GET: Accounting/EditJournalEntry/5
        public async Task<IActionResult> EditJournalEntry(Guid id)
        {
            var journalEntry = await _context.JournalEntries.FindAsync(id);
            if (journalEntry == null)
            {
                return NotFound();
            }

            ViewBag.Accounts = await _context.ChartOfAccounts.Where(c => c.IsActive == true).OrderBy(c => c.AccountCode).ToListAsync();
            ViewBag.EntryTypes = new List<string> { "فاکتور فروش", "فاکتور خرید", "پرداخت", "دریافت", "تسویه" };
            ViewBag.StatusList = new List<string> { "پیش‌نویس", "تایید شده", "لغو شده" };

            return View(journalEntry);
        }

        // POST: Accounting/EditJournalEntry/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditJournalEntry(Guid id, JournalEntry journalEntry)
        {
            if (id != journalEntry.EntryId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingEntry = await _context.JournalEntries.FindAsync(id);
                    if (existingEntry == null)
                    {
                        return NotFound();
                    }

                    // Keep the original creation date
                    journalEntry.CreatedDate = existingEntry.CreatedDate;

                    _context.Entry(existingEntry).CurrentValues.SetValues(journalEntry);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "اطلاعات سند حسابداری با موفقیت به‌روزرسانی شد";
                    return RedirectToAction(nameof(AccountingReport));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!JournalEntryExists(journalEntry.EntryId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            ViewBag.Accounts = await _context.ChartOfAccounts.Where(c => c.IsActive == true).OrderBy(c => c.AccountCode).ToListAsync();
            ViewBag.EntryTypes = new List<string> { "فاکتور فروش", "فاکتور خرید", "پرداخت", "دریافت", "تسویه" };
            ViewBag.StatusList = new List<string> { "پیش‌نویس", "تایید شده", "لغو شده" };

            return View(journalEntry);
        }

        // POST: Accounting/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var journalEntry = await _context.JournalEntries.FindAsync(id);
            if (journalEntry == null)
            {
                TempData["ErrorMessage"] = "سند حسابداری مورد نظر یافت نشد";
                return RedirectToAction(nameof(AccountingReport));
            }

            // Check if entry is approved
            if (journalEntry.Status == "تایید شده")
            {
                TempData["ErrorMessage"] = "سند تایید شده قابل حذف نیست";
                return RedirectToAction(nameof(AccountingReport));
            }

            try
            {
                _context.JournalEntries.Remove(journalEntry);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "سند حسابداری با موفقیت حذف شد";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "خطا در حذف سند حسابداری: " + ex.Message;
            }

            return RedirectToAction(nameof(AccountingReport));
        }

        // GET: Accounting/ChartOfAccounts
        public async Task<IActionResult> ChartOfAccounts(int? page, string searchKey, string accountType)
        {
            var setting = await ReadSettingAsync(_context);
            int pageSize = Convert.ToInt32(setting.NumberPerPage ?? 10);
            int pageNumber = page ?? 1;

            IQueryable<ChartOfAccount> query = _context.ChartOfAccounts
                .Include(c => c.Category);

            if (!string.IsNullOrWhiteSpace(searchKey))
            {
                query = query.Where(c => c.AccountName.Contains(searchKey) ||
                                     c.AccountCode.Contains(searchKey))
                            .OrderBy(c => c.AccountCode);
            }

            if (!string.IsNullOrWhiteSpace(accountType))
            {
                query = query.Where(c => c.AccountType == accountType);
            }

            int total = await query.CountAsync();
            var items = await query
                .OrderBy(c => c.AccountCode)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var paginatedList = new PaginatedList<ChartOfAccount>(items, total, pageNumber, pageSize);

            ViewBag.AccountTypes = new List<string> { "دارایی", "بدهی", "حقوق صاحبان سهام", "درآمد", "هزینه" };

            return View(paginatedList);
        }

        private bool JournalEntryExists(Guid id)
        {
            return _context.JournalEntries.Any(e => e.EntryId == id);
        }
    }
}