using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mehrsam_Darou.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using static Mehrsam_Darou.Helper.Helper;

namespace Mehrsam_Darou.Controllers
{
    public class FinishedGoodsBatchController : BaseController
    {
        private readonly DarouAppContext _context;

        public FinishedGoodsBatchController(DarouAppContext context) : base(context)
        {
            _context = context;
        }

        // GET: FinishedGoodsBatch/FinishedGoodsBatchList
        public async Task<IActionResult> FinishedGoodsBatchList(int? page, string searchKey)
        {
            var setting = await ReadSettingAsync(_context);
            int pageSize = Convert.ToInt32(setting.NumberPerPage ?? 10);
            int pageNumber = page ?? 1;

            IQueryable<FinishedGoodsBatch> query = _context.FinishedGoodsBatches
                .Include(f => f.Medicine)
                .Include(f => f.Unit)
                .Include(f => f.Location)
                .Include(f => f.Order);

            if (!string.IsNullOrWhiteSpace(searchKey))
            {
                query = query.Where(f => f.BatchNumber.Contains(searchKey) ||
                                     f.Medicine.BrandName.Contains(searchKey) ||
                                     f.Medicine.MedicineCode.Contains(searchKey) ||
                                     f.Status.Contains(searchKey))
                            .OrderBy(f => f.BatchNumber);
            }

            int total = await query.CountAsync();
            var items = await query
                .OrderByDescending(f => f.ManufactureDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var paginatedList = new PaginatedList<FinishedGoodsBatch>(items, total, pageNumber, pageSize);

            return View(paginatedList);
        }

        // GET: FinishedGoodsBatch/AddFinishedGoodsBatch
        public async Task<IActionResult> AddFinishedGoodsBatch()
        {
            await LoadViewBagData();
            return View(new FinishedGoodsBatch
            {
                ManufactureDate = DateOnly.FromDateTime(DateTime.Now),
                ExpiryDate = DateOnly.FromDateTime(DateTime.Now.AddMonths(24)),
                Status = "قرنطینه"
            });
        }

        // POST: FinishedGoodsBatch/AddFinishedGoodsBatch
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddFinishedGoodsBatch(FinishedGoodsBatch finishedGoodsBatch)
        {
            // Remove navigation properties from model validation
            ModelState.Remove("Medicine");
            ModelState.Remove("Unit");
            ModelState.Remove("Location");
            ModelState.Remove("Order");

            if (ModelState.IsValid)
            {
                try
                {
                    // Check if batch number already exists for this medicine
                    if (await _context.FinishedGoodsBatches.AnyAsync(f =>
                        f.MedicineId == finishedGoodsBatch.MedicineId &&
                        f.BatchNumber == finishedGoodsBatch.BatchNumber))
                    {
                        TempData["ErrorMessage"] = "شماره بچ برای این دارو قبلاً ثبت شده است";
                        await LoadViewBagData();
                        return View(finishedGoodsBatch);
                    }

                    finishedGoodsBatch.BatchId = Guid.NewGuid();

                    _context.Add(finishedGoodsBatch);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "بچ محصول نهایی جدید با موفقیت ایجاد شد";
                    return RedirectToAction(nameof(FinishedGoodsBatchList));
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "خطا در ایجاد بچ محصول نهایی: " + ex.Message;
                }
            }

            await LoadViewBagData();
            return View(finishedGoodsBatch);
        }

        // GET: FinishedGoodsBatch/EditFinishedGoodsBatch/5
        public async Task<IActionResult> EditFinishedGoodsBatch(Guid id)
        {
            var finishedGoodsBatch = await _context.FinishedGoodsBatches.FindAsync(id);
            if (finishedGoodsBatch == null)
            {
                return NotFound();
            }

            await LoadViewBagData();
            return View(finishedGoodsBatch);
        }

        // POST: FinishedGoodsBatch/EditFinishedGoodsBatch/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditFinishedGoodsBatch(Guid id, FinishedGoodsBatch finishedGoodsBatch)
        {
            if (id != finishedGoodsBatch.BatchId)
            {
                return NotFound();
            }

            // Remove navigation properties from model validation
            ModelState.Remove("Medicine");
            ModelState.Remove("Unit");
            ModelState.Remove("Location");
            ModelState.Remove("Order");

            if (ModelState.IsValid)
            {
                try
                {
                    // Check if batch number already exists for this medicine (excluding current record)
                    if (await _context.FinishedGoodsBatches.AnyAsync(f =>
                        f.BatchId != id &&
                        f.MedicineId == finishedGoodsBatch.MedicineId &&
                        f.BatchNumber == finishedGoodsBatch.BatchNumber))
                    {
                        TempData["ErrorMessage"] = "شماره بچ برای این دارو قبلاً ثبت شده است";
                        await LoadViewBagData();
                        return View(finishedGoodsBatch);
                    }

                    _context.Update(finishedGoodsBatch);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "اطلاعات بچ محصول نهایی با موفقیت به‌روزرسانی شد";
                    return RedirectToAction(nameof(FinishedGoodsBatchList));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!FinishedGoodsBatchExists(finishedGoodsBatch.BatchId))
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
            return View(finishedGoodsBatch);
        }

        // POST: FinishedGoodsBatch/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var finishedGoodsBatch = await _context.FinishedGoodsBatches.FindAsync(id);
            if (finishedGoodsBatch == null)
            {
                TempData["ErrorMessage"] = "بچ محصول نهایی مورد نظر یافت نشد";
                return RedirectToAction(nameof(FinishedGoodsBatchList));
            }

            // Check if batch is used in sales invoices or shipments
            bool hasInvoiceItems = await _context.SalesInvoiceItems.AnyAsync(s => s.BatchId == id);
            bool hasShipmentItems = await _context.ShipmentItems.AnyAsync(s => s.BatchId == id);

            if (hasInvoiceItems || hasShipmentItems)
            {
                TempData["ErrorMessage"] = "این بچ در فاکتورها یا حمل و نقل استفاده شده و قابل حذف نیست";
                return RedirectToAction(nameof(FinishedGoodsBatchList));
            }

            try
            {
                _context.FinishedGoodsBatches.Remove(finishedGoodsBatch);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "بچ محصول نهایی با موفقیت حذف شد";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "خطا در حذف بچ محصول نهایی: " + ex.Message;
            }

            return RedirectToAction(nameof(FinishedGoodsBatchList));
        }

        private bool FinishedGoodsBatchExists(Guid id)
        {
            return _context.FinishedGoodsBatches.Any(e => e.BatchId == id);
        }

        private async Task LoadViewBagData()
        {
            ViewBag.Medicines = await _context.Medicines
                .Where(m => m.IsActive == true)
                .OrderBy(m => m.BrandName)
                .ToListAsync();

            ViewBag.Units = await _context.Units
                .Where(u => u.IsActive == true)
                .OrderBy(u => u.UnitName)
                .ToListAsync();

            ViewBag.Locations = await _context.StorageLocations
                .Where(l => l.IsActive == true)
                .OrderBy(l => l.LocationName)
                .ToListAsync();

            ViewBag.ProductionOrders = await _context.ProductionOrders
                .OrderBy(p => p.OrderNumber)
                .ToListAsync();
        }
    }
}