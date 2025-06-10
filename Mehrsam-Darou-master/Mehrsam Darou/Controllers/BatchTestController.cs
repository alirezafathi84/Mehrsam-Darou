using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mehrsam_Darou.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using static Mehrsam_Darou.Helper.Helper;

namespace Mehrsam_Darou.Controllers
{
    public class BatchTestController : BaseController
    {
        private readonly DarouAppContext _context;

        public BatchTestController(DarouAppContext context) : base(context)
        {
            _context = context;
        }

        // GET: BatchTest/BatchTestList
        public async Task<IActionResult> BatchTestList(int? page, string searchKey)
        {
            var setting = await ReadSettingAsync(_context);
            int pageSize = Convert.ToInt32(setting.NumberPerPage ?? 10);
            int pageNumber = page ?? 1;

            IQueryable<BatchTest> query = _context.BatchTests
                .Include(bt => bt.Product)
                .Include(bt => bt.Test);

            if (!string.IsNullOrWhiteSpace(searchKey))
            {
                query = query.Where(bt => bt.TestNumber.Contains(searchKey) ||
                                     bt.BatchNumber.Contains(searchKey) ||
                                     bt.Product.BrandName.Contains(searchKey) ||
                                     bt.Test.TestName.Contains(searchKey))
                            .OrderByDescending(bt => bt.CreatedDate);
            }
            else
            {
                query = query.OrderByDescending(bt => bt.CreatedDate);
            }

            int total = await query.CountAsync();
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var paginatedList = new PaginatedList<BatchTest>(items, total, pageNumber, pageSize);

            return View(paginatedList);
        }

        // GET: BatchTest/AddBatchTest
        public async Task<IActionResult> AddBatchTest()
        {
            ViewBag.Products = await _context.Medicines
                .Where(m => m.IsActive == true)
                .Select(m => new { m.MedicineId, m.BrandName })
                .ToListAsync();

            ViewBag.Tests = await _context.QcTests
                .Where(t => t.IsActive == true)
                .Select(t => new { t.TestId, t.TestName })
                .ToListAsync();

            return View(new BatchTest
            {
                IsActive = true,
                CreatedDate = DateTime.Now,
                TestStatus = "برنامه‌ریزی شده",
                TestPriority = 3
            });
        }

        // POST: BatchTest/AddBatchTest
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddBatchTest(BatchTest batchTest)
        {
            // Remove navigation property validation errors
            ModelState.Remove("Product");
            ModelState.Remove("Test");

            if (ModelState.IsValid)
            {
                try
                {
                    if (await _context.BatchTests.AnyAsync(bt => bt.TestNumber == batchTest.TestNumber))
                    {
                        TempData["ErrorMessage"] = "آزمون با این شماره قبلاً ثبت شده است";
                        await LoadViewBagData();
                        return View(batchTest);
                    }

                    batchTest.BatchTestId = Guid.NewGuid();
                    batchTest.CreatedDate = DateTime.Now;

                    _context.Add(batchTest);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "آزمون جدید با موفقیت ایجاد شد";
                    return RedirectToAction(nameof(BatchTestList));
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "خطا در ایجاد آزمون: " + ex.Message;
                }
            }

            await LoadViewBagData();
            return View(batchTest);
        }

        // GET: BatchTest/EditBatchTest/5
        public async Task<IActionResult> EditBatchTest(Guid id)
        {
            var batchTest = await _context.BatchTests.FindAsync(id);
            if (batchTest == null)
            {
                return NotFound();
            }

            await LoadViewBagData();
            return View(batchTest);
        }

        // POST: BatchTest/EditBatchTest/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditBatchTest(Guid id, BatchTest batchTest)
        {
            if (id != batchTest.BatchTestId)
            {
                return NotFound();
            }

            // Remove navigation property validation errors
            ModelState.Remove("Product");
            ModelState.Remove("Test");

            if (ModelState.IsValid)
            {
                try
                {
                    if (await _context.BatchTests.AnyAsync(bt =>
                        bt.BatchTestId != id &&
                        bt.TestNumber == batchTest.TestNumber))
                    {
                        TempData["ErrorMessage"] = "آزمون با این شماره قبلاً ثبت شده است";
                        await LoadViewBagData();
                        return View(batchTest);
                    }

                    var existingBatchTest = await _context.BatchTests.FindAsync(id);
                    if (existingBatchTest == null)
                    {
                        return NotFound();
                    }

                    // Keep the original creation date
                    batchTest.CreatedDate = existingBatchTest.CreatedDate;
                    batchTest.LastModifiedDate = DateTime.Now;

                    _context.Entry(existingBatchTest).CurrentValues.SetValues(batchTest);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "اطلاعات آزمون با موفقیت به‌روزرسانی شد";
                    return RedirectToAction(nameof(BatchTestList));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BatchTestExists(batchTest.BatchTestId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            await LoadViewBagData();
            return View(batchTest);
        }

        // POST: BatchTest/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var batchTest = await _context.BatchTests.FindAsync(id);
            if (batchTest == null)
            {
                TempData["ErrorMessage"] = "آزمون مورد نظر یافت نشد";
                return RedirectToAction(nameof(BatchTestList));
            }

            try
            {
                _context.BatchTests.Remove(batchTest);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "آزمون با موفقیت حذف شد";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "خطا در حذف آزمون: " + ex.Message;
            }

            return RedirectToAction(nameof(BatchTestList));
        }

        private bool BatchTestExists(Guid id)
        {
            return _context.BatchTests.Any(e => e.BatchTestId == id);
        }

        private async Task LoadViewBagData()
        {
            ViewBag.Products = await _context.Medicines
                .Where(m => m.IsActive == true)
                .Select(m => new { m.MedicineId, m.BrandName })
                .ToListAsync();

            ViewBag.Tests = await _context.QcTests
                .Where(t => t.IsActive == true)
                .Select(t => new { t.TestId, t.TestName })
                .ToListAsync();
        }
    }
}