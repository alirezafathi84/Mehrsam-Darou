using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mehrsam_Darou.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using static Mehrsam_Darou.Helper.Helper;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Mehrsam_Darou.Controllers
{
    public class MaterialBatchController : BaseController
    {
        private readonly DarouAppContext _context;

        public MaterialBatchController(DarouAppContext context) : base(context)
        {
            _context = context;
        }

        public async Task<IActionResult> MaterialBatchList(int? page, string SearchKey)
        {
            var setting = await ReadSettingAsync(_context);
            int pageSize = Convert.ToInt32(setting.NumberPerPage ?? 10);
            int pageNumber = page ?? 1;

            IQueryable<MaterialBatch> query = _context.MaterialBatches
                .Include(m => m.Material)
                .Include(m => m.Unit)
                .Include(m => m.Location);

            if (!string.IsNullOrWhiteSpace(SearchKey))
            {
                query = query.Where(m =>
                    m.BatchNumber.Contains(SearchKey) ||
                    m.Material.MaterialName.Contains(SearchKey))
                    .OrderBy(m => m.BatchNumber);
            }

            int total = await query.CountAsync();

            var Items = await query
                .OrderBy(m => m.BatchNumber)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var paginatedMaterialBatches = new PaginatedList<MaterialBatch>(Items, total, pageNumber, pageSize);

            return View(paginatedMaterialBatches);
        }

        public async Task<IActionResult> AddMaterialBatch()
        {
            var model = new MaterialBatch
            {
                BatchId = Guid.NewGuid(),
                Status = "Released"
            };

            await PopulateDropdowns();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMaterialBatch(MaterialBatch materialBatch)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Add(materialBatch);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "بچ مواد اولیه با موفقیت اضافه شد";
                    return RedirectToAction(nameof(MaterialBatchList));
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "خطا در ذخیره سازی: " + ex.Message;
                }
            }

            await PopulateDropdowns();
            return View(materialBatch);
        }

        public async Task<IActionResult> EditMaterialBatch(Guid id)
        {
            var materialBatch = await _context.MaterialBatches
                .Include(m => m.Material)
                .Include(m => m.Unit)
                .Include(m => m.Location)
                .FirstOrDefaultAsync(m => m.BatchId == id);

            if (materialBatch == null)
            {
                return NotFound();
            }

            await PopulateDropdowns();
            return View(materialBatch);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditMaterialBatch(Guid id, MaterialBatch materialBatch)
        {
            if (id != materialBatch.BatchId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(materialBatch);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "اطلاعات بچ مواد اولیه با موفقیت به‌روزرسانی شد";
                    return RedirectToAction(nameof(MaterialBatchList));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MaterialBatchExists(materialBatch.BatchId))
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
            return View(materialBatch);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMaterialBatch(Guid id)
        {
            var materialBatch = await _context.MaterialBatches.FindAsync(id);
            if (materialBatch == null)
            {
                TempData["ErrorMessage"] = "بچ مواد اولیه مورد نظر یافت نشد";
                return RedirectToAction(nameof(MaterialBatchList));
            }

            try
            {
                _context.MaterialBatches.Remove(materialBatch);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "بچ مواد اولیه با موفقیت حذف شد";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "خطا در حذف بچ مواد اولیه: " + ex.Message;
            }

            return RedirectToAction(nameof(MaterialBatchList));
        }

        private bool MaterialBatchExists(Guid id)
        {
            return _context.MaterialBatches.Any(e => e.BatchId == id);
        }

        private async Task PopulateDropdowns()
        {
            // Active materials
            var materials = await _context.RawMaterials
                .Where(m => m.IsActive == true)
                .OrderBy(m => m.MaterialName)
                .ToListAsync();

            ViewBag.Materials = new SelectList(materials, "MaterialId", "MaterialName");

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

            // Status options
            ViewBag.StatusOptions = new SelectList(new[]
            {
                new { Value = "Released", Text = "منتشر شده" },
                new { Value = "Quarantine", Text = "قرنطینه" },
                new { Value = "Rejected", Text = "رد شده" },
                new { Value = "Consumed", Text = "مصرف شده" }
            }, "Value", "Text");
        }
    }
}