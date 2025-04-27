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
    public class RawMaterialController : BaseController
    {
        private readonly DarouAppContext _context;

        public RawMaterialController(DarouAppContext context) : base(context)
        {
            _context = context;
        }

        // GET: RawMaterial/RawMaterialList
        public async Task<IActionResult> RawMaterialList(int? page, string searchKey)
        {
            var setting = await ReadSettingAsync(_context);
            int pageSize = Convert.ToInt32(setting.NumberPerPage ?? 10);
            int pageNumber = page ?? 1;

            IQueryable<RawMaterial> query = _context.RawMaterials
                .Include(r => r.Category)
                .Include(r => r.Unit);

            if (!string.IsNullOrWhiteSpace(searchKey))
            {
                query = query.Where(r => r.MaterialName.Contains(searchKey) ||
                                     r.MaterialCode.Contains(searchKey))
                            .OrderBy(r => r.MaterialName);
            }

            int total = await query.CountAsync();
            var items = await query
                .OrderBy(r => r.MaterialName)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var paginatedList = new PaginatedList<RawMaterial>(items, total, pageNumber, pageSize);

            return View(paginatedList);
        }

        // GET: RawMaterial/AddRawMaterial
        public async Task<IActionResult> AddRawMaterial()
        {
            await PopulateRawMaterialDropdowns();
            return View(new RawMaterial { IsActive = true });
        }

        // POST: RawMaterial/AddRawMaterial
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddRawMaterial(RawMaterial rawMaterial)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    if (await _context.RawMaterials.AnyAsync(r => r.MaterialCode == rawMaterial.MaterialCode))
                    {
                        TempData["ErrorMessage"] = "ماده اولیه با این کد قبلاً ثبت شده است";
                        await PopulateRawMaterialDropdowns();
                        return View(rawMaterial);
                    }

                    _context.Add(rawMaterial);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "ماده اولیه جدید با موفقیت ایجاد شد";
                    return RedirectToAction(nameof(RawMaterialList));
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "خطا در ایجاد ماده اولیه: " + ex.Message;
                }
            }

            await PopulateRawMaterialDropdowns();
            return View(rawMaterial);
        }

        // GET: RawMaterial/EditRawMaterial/5
        public async Task<IActionResult> EditRawMaterial(Guid id)
        {
            var rawMaterial = await _context.RawMaterials.FindAsync(id);
            if (rawMaterial == null)
            {
                return NotFound();
            }

            await PopulateRawMaterialDropdowns();
            return View(rawMaterial);
        }

        // POST: RawMaterial/EditRawMaterial/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditRawMaterial(Guid id, RawMaterial rawMaterial)
        {
            if (id != rawMaterial.MaterialId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (await _context.RawMaterials.AnyAsync(r =>
                        r.MaterialId != id &&
                        r.MaterialCode == rawMaterial.MaterialCode))
                    {
                        TempData["ErrorMessage"] = "ماده اولیه با این کد قبلاً ثبت شده است";
                        await PopulateRawMaterialDropdowns();
                        return View(rawMaterial);
                    }

                    _context.Update(rawMaterial);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "اطلاعات ماده اولیه با موفقیت به‌روزرسانی شد";
                    return RedirectToAction(nameof(RawMaterialList));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RawMaterialExists(rawMaterial.MaterialId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            await PopulateRawMaterialDropdowns();
            return View(rawMaterial);
        }

        // POST: RawMaterial/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var rawMaterial = await _context.RawMaterials.FindAsync(id);
            if (rawMaterial == null)
            {
                TempData["ErrorMessage"] = "ماده اولیه مورد نظر یافت نشد";
                return RedirectToAction(nameof(RawMaterialList));
            }

            // Check if material is used in any BOMs
            bool isUsed = await _context.MedicineBoms.AnyAsync(b => b.MaterialId == id);
            if (isUsed)
            {
                TempData["ErrorMessage"] = "این ماده اولیه در BOM داروها استفاده شده و قابل حذف نیست";
                return RedirectToAction(nameof(RawMaterialList));
            }

            try
            {
                _context.RawMaterials.Remove(rawMaterial);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "ماده اولیه با موفقیت حذف شد";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "خطا در حذف ماده اولیه: " + ex.Message;
            }

            return RedirectToAction(nameof(RawMaterialList));
        }

        private bool RawMaterialExists(Guid id)
        {
            return _context.RawMaterials.Any(e => e.MaterialId == id);
        }

        private async Task PopulateRawMaterialDropdowns()
        {
            // Active material categories
            var categories = await _context.MaterialCategories
                .Where(c => c.IsActive == true)
                .OrderBy(c => c.CategoryName)
                .ToListAsync();

            ViewBag.Categories = new SelectList(categories, "CategoryId", "CategoryName");

            // Active units
            var units = await _context.Units
                .Where(u => u.IsActive == true)
                .OrderBy(u => u.UnitName)
                .ToListAsync();

            ViewBag.Units = new SelectList(units, "UnitId", "UnitName");
        }
    }
}