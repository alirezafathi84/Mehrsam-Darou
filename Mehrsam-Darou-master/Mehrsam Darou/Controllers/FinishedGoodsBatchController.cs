using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mehrsam_Darou.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using static Mehrsam_Darou.Helper.Helper;
using Microsoft.AspNetCore.Mvc.Rendering;
using DinkToPdf;
using System.Globalization;

namespace Mehrsam_Darou.Controllers
{
    public class FinishedGoodsBatchController : BaseController
    {
        private readonly DarouAppContext _context;

        public FinishedGoodsBatchController(DarouAppContext context) : base(context)
        {
            _context = context;
        }

        public async Task<IActionResult> FinishedGoodsBatchList(int? page, string SearchKey)
        {
            var setting = await ReadSettingAsync(_context);
            int pageSize = Convert.ToInt32(setting.NumberPerPage ?? 10);
            int pageNumber = page ?? 1;

            IQueryable<FinishedGoodsBatch> query = _context.FinishedGoodsBatches
                .Include(f => f.Medicine)
                .Include(f => f.Unit)
                .Include(f => f.Location)
                .Include(f => f.Order);

            if (!string.IsNullOrWhiteSpace(SearchKey))
            {
                query = query.Where(f =>
                    f.BatchNumber.Contains(SearchKey) ||
                    f.Medicine.BrandName.Contains(SearchKey))
                    .OrderBy(f => f.BatchNumber);
            }

            int total = await query.CountAsync();

            var Items = await query
                .OrderBy(f => f.BatchNumber)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var paginatedFinishedGoodsBatches = new PaginatedList<FinishedGoodsBatch>(Items, total, pageNumber, pageSize);

            return View(paginatedFinishedGoodsBatches);
        }

        public async Task<IActionResult> AddFinishedGoodsBatch()
        {
            var model = new FinishedGoodsBatch
            {
                BatchId = Guid.NewGuid(),
                Status = "Released"
            };

            await PopulateDropdowns();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddFinishedGoodsBatch(FinishedGoodsBatch finishedGoodsBatch)
        {

                try
                {
                    _context.Add(finishedGoodsBatch);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "بچ محصول نهایی با موفقیت اضافه شد";
                    return RedirectToAction(nameof(FinishedGoodsBatchList));
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "خطا در ذخیره سازی: " + ex.Message;
                }
          

            await PopulateDropdowns();
            return View(finishedGoodsBatch);
        }

        public async Task<IActionResult> EditFinishedGoodsBatch(Guid id)
        {
            var finishedGoodsBatch = await _context.FinishedGoodsBatches
                .Include(f => f.Medicine)
                .Include(f => f.Unit)
                .Include(f => f.Location)
                .Include(f => f.Order)
                .FirstOrDefaultAsync(f => f.BatchId == id);

            if (finishedGoodsBatch == null)
            {
                return NotFound();
            }

            await PopulateDropdowns();
            return View(finishedGoodsBatch);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditFinishedGoodsBatch(Guid id, FinishedGoodsBatch finishedGoodsBatch)
        {
            if (id != finishedGoodsBatch.BatchId)
            {
                return NotFound();
            }

            // Ensure MedicineId is valid
            if (finishedGoodsBatch.MedicineId == Guid.Empty)
            {
                ModelState.AddModelError("MedicineId", "لطفاً دارو را انتخاب کنید");
                await PopulateDropdowns();
                return View(finishedGoodsBatch);
            }

            try
            {
                // Attach the existing entity and update modified fields
                var existingBatch = await _context.FinishedGoodsBatches
                    .FirstOrDefaultAsync(f => f.BatchId == id);

                if (existingBatch == null)
                {
                    return NotFound();
                }

                // Update only editable fields (exclude BatchNumber/MedicineId if needed)
                existingBatch.Quantity = finishedGoodsBatch.Quantity;
                existingBatch.UnitId = finishedGoodsBatch.UnitId;
                existingBatch.LocationId = finishedGoodsBatch.LocationId;
                existingBatch.ManufactureDate = finishedGoodsBatch.ManufactureDate;
                existingBatch.ExpiryDate = finishedGoodsBatch.ExpiryDate;
                existingBatch.Status = finishedGoodsBatch.Status;

                _context.Update(existingBatch);
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteFinishedGoodsBatch(Guid id)
        {
            var finishedGoodsBatch = await _context.FinishedGoodsBatches.FindAsync(id);
            if (finishedGoodsBatch == null)
            {
                TempData["ErrorMessage"] = "بچ محصول نهایی مورد نظر یافت نشد";
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

        private async Task PopulateDropdowns()
        {
            // Active medicines
            var medicines = await _context.Medicines
                .Where(m => m.IsActive == true)
                .OrderBy(m => m.BrandName)
                .ToListAsync();

            ViewBag.Medicines = new SelectList(medicines, "MedicineId", "BrandName");

            // Active units
            var units = await _context.Units
                .Where(u => u.IsActive == true)
                .OrderBy(u => u.UnitName)
                .ToListAsync();

            ViewBag.Units = new SelectList(units, "UnitId", "UnitName");

            // Active locations
            var locations = await _context.StorageLocations
                .Where(l => l.IsActive == true)
                .OrderBy(l => l.LocationName)
                .ToListAsync();

            ViewBag.Locations = new SelectList(locations, "LocationId", "LocationName");

            // Active production orders
            var orders = await _context.ProductionOrders
                .Where(o => o.Status == "In Progress" || o.Status == "Planned")
                .OrderBy(o => o.OrderNumber)
                .ToListAsync();

            ViewBag.Orders = new SelectList(orders, "OrderId", "OrderNumber");

            // Status options
            ViewBag.StatusOptions = new SelectList(new[]
            {
                new { Value = "Released", Text = "منتشر شده" },
                new { Value = "Quarantine", Text = "قرنطینه" },
                new { Value = "Rejected", Text = "رد شده" }
            }, "Value", "Text");
        }


        //public async Task<IActionResult> ExportToPdf()
        //{
        //    var batches = await _context.FinishedGoodsBatches
        //        .Include(f => f.Medicine)
        //        .Include(f => f.Unit)
        //        .ToListAsync();

        //    var html = await this.RenderViewAsync("_PrintTemplate", batches, true);

        //    var converter = new BasicConverter(new PdfTools());
        //    var doc = new HtmlToPdfDocument()
        //    {
        //        GlobalSettings = {
        //    ColorMode = ColorMode.Color,
        //    Orientation = Orientation.Portrait,
        //    PaperSize = PaperKind.A4,
        //},
        //        Objects = {
        //    new ObjectSettings() {
        //        PagesCount = true,
        //        HtmlContent = html,
        //        WebSettings = { DefaultEncoding = "utf-8" },
        //        HeaderSettings = { FontSize = 9, Right = "صفحه [page] از [toPage]", Line = true },
        //    }
        //}
        //    };

        //    var pdf = converter.Convert(doc);
        //    return File(pdf, "application/pdf", "FinishedGoodsBatches.pdf");
        //}



    }        public static class DateTimeExtensions
        {
            public static string ToPersianDat1e(this DateTime date)
            {
                PersianCalendar pc = new PersianCalendar();
                return $"{pc.GetYear(date)}/{pc.GetMonth(date):00}/{pc.GetDayOfMonth(date):00}";
            }
        }
}