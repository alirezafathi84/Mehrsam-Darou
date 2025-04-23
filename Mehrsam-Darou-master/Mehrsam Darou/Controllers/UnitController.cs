using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mehrsam_Darou.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using static Mehrsam_Darou.Helper.Helper;
using Microsoft.AspNetCore.Mvc.Rendering; // for SelectList

namespace Mehrsam_Darou.Controllers
{
    public class UnitController : BaseController
    {
        private readonly DarouAppContext _context;

        public UnitController(DarouAppContext context) : base(context)
        {
            _context = context;
        }

        public async Task<IActionResult> UnitList(int? page, string SearchKey)
        {
            // Set common view data and get the page size
            var setting = await ReadSettingAsync(_context);
            int pageSize = Convert.ToInt32(setting.NumberPerPage ?? 10);
            int pageNumber = page ?? 1;

            // Base query for fetching units
            IQueryable<Unit> query = _context.Units
                .Include(u => u.UnitType); // Include UnitType for display

            // Apply search filter if SearchKey is provided
            if (!string.IsNullOrWhiteSpace(SearchKey))
            {
                query = query.Where(u =>
                    u.UnitName.Contains(SearchKey) ||
                    u.UnitSymbol.Contains(SearchKey) ||
                    u.UnitType.TypeName.Contains(SearchKey))
                    .OrderBy(u => u.UnitName);
            }

            // Get total count after filtering
            int total = await query.CountAsync();

            // Fetch paginated results
            var Items = await query
                .OrderBy(u => u.UnitName)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Create the paginated list
            var paginatedUnits = new PaginatedList<Unit>(Items, total, pageNumber, pageSize);

            // Pass paginated list to the view
            return View(paginatedUnits);
        }

        // GET: Unit/Add
        public async Task<IActionResult> AddUnit()
        {
            var unitTypes = await _context.UnitTypes.ToListAsync();

            // If the list is empty, you might want to handle this gracefully 
            // or at least assign an empty SelectList, but usually the list should have items.
            ViewBag.UnitTypes = new SelectList(unitTypes, "UnitTypeId", "TypeName");

            return View(new Unit());  // Pass a new Unit instance to the view to avoid null Model
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddUnit(Unit unit)
        {
            if (ModelState.IsValid)
            {
                unit.UnitId = Guid.NewGuid();
                _context.Add(unit);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "واحد اندازه‌گیری با موفقیت اضافه شد";
                return RedirectToAction(nameof(UnitList));
            }

            var unitTypes = await _context.UnitTypes.ToListAsync();
            ViewBag.UnitTypes = new SelectList(unitTypes, "UnitTypeId", "TypeName");

            TempData["ErrorMessage"] = "لطفاً اطلاعات را صحیح وارد نمایید";
            return View(unit);
        }

        // GET: Unit/Edit/5
        public async Task<IActionResult> EditUnit(Guid id)
        {
            if (id == Guid.Empty)
                return NotFound();

            var unit = await _context.Units.FindAsync(id);
            if (unit == null)
                return NotFound();

            var unitTypes = await _context.UnitTypes.ToListAsync();
            ViewBag.UnitTypes = new SelectList(unitTypes, "UnitTypeId", "TypeName");

            return View(unit);
        }

        // POST: Unit/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUnit(Unit unit)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(unit);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "ویرایش واحد با موفقیت انجام شد";
                    return RedirectToAction(nameof(UnitList));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Units.Any(e => e.UnitId == unit.UnitId))
                        return NotFound();

                    throw;
                }
            }

            var unitTypes = await _context.UnitTypes.ToListAsync();
            ViewBag.UnitTypes = new SelectList(unitTypes, "UnitTypeId", "TypeName");
            TempData["ErrorMessage"] = "لطفاً اطلاعات را صحیح وارد نمایید";
            return View(unit);
        }

        // POST: Unit/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUnit(Guid id)
        {
            var unit = await _context.Units.FindAsync(id);
            if (unit == null)
            {
                TempData["ErrorMessage"] = "واحد اندازه‌گیری مورد نظر یافت نشد";
                return RedirectToAction(nameof(UnitList));
            }

            // Check if unit is being used before deleting
            var isUsed = await _context.RawMaterials.AnyAsync(m => m.UnitId == id) ||
                         await _context.Medicines.AnyAsync(m => m.StrengthUnitId == id);

            if (isUsed)
            {
                TempData["ErrorMessage"] = "این واحد در حال استفاده است و نمی‌توان آن را حذف کرد";
                return RedirectToAction(nameof(UnitList));
            }

            try
            {
                _context.Units.Remove(unit);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "واحد اندازه‌گیری با موفقیت حذف شد";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "خطا در حذف واحد اندازه‌گیری: " + ex.Message;
            }

            return RedirectToAction(nameof(UnitList));
        }

        private bool UnitExists(Guid id)
        {
            return _context.Units.Any(e => e.UnitId == id);
        }
    }
}