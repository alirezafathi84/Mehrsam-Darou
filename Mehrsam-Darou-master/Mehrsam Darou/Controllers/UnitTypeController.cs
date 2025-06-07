using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mehrsam_Darou.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using static Mehrsam_Darou.Helper.Helper;

namespace Mehrsam_Darou.Controllers
{
    public class UnitTypeController : BaseController
    {
        private readonly DarouAppContext _context;

        public UnitTypeController(DarouAppContext context) : base(context)
        {
            _context = context;
        }

        // GET: UnitType/UnitTypeList
        public async Task<IActionResult> UnitTypeList(int? page, string searchKey)
        {
            var setting = await ReadSettingAsync(_context);
            int pageSize = Convert.ToInt32(setting.NumberPerPage ?? 10);
            int pageNumber = page ?? 1;

            IQueryable<UnitType> query = _context.UnitTypes;

            if (!string.IsNullOrWhiteSpace(searchKey))
            {
                query = query.Where(u => u.TypeName.Contains(searchKey) ||
                                     u.Description.Contains(searchKey))
                            .OrderBy(u => u.TypeName);
            }

            int total = await query.CountAsync();
            var items = await query
                .OrderBy(u => u.TypeName)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var paginatedList = new PaginatedList<UnitType>(items, total, pageNumber, pageSize);

            return View(paginatedList);
        }

        // GET: UnitType/AddUnitType
        public IActionResult AddUnitType()
        {
            return View(new UnitType { IsSystem = false });
        }

        // POST: UnitType/AddUnitType
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddUnitType(UnitType unitType)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    if (await _context.UnitTypes.AnyAsync(u => u.TypeName == unitType.TypeName))
                    {
                        TempData["ErrorMessage"] = "نوع واحد با این نام قبلاً ثبت شده است";
                        return View(unitType);
                    }

                    unitType.UnitTypeId = Guid.NewGuid();

                    _context.Add(unitType);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "نوع واحد جدید با موفقیت ایجاد شد";
                    return RedirectToAction(nameof(UnitTypeList));
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "خطا در ایجاد نوع واحد: " + ex.Message;
                }
            }

            return View(unitType);
        }

        // GET: UnitType/EditUnitType/5
        public async Task<IActionResult> EditUnitType(Guid id)
        {
            var unitType = await _context.UnitTypes.FindAsync(id);
            if (unitType == null)
            {
                return NotFound();
            }

            return View(unitType);
        }

        // POST: UnitType/EditUnitType/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUnitType(Guid id, UnitType unitType)
        {
            if (id != unitType.UnitTypeId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (await _context.UnitTypes.AnyAsync(u =>
                        u.UnitTypeId != id &&
                        u.TypeName == unitType.TypeName))
                    {
                        TempData["ErrorMessage"] = "نوع واحد با این نام قبلاً ثبت شده است";
                        return View(unitType);
                    }

                    var existingUnitType = await _context.UnitTypes.FindAsync(id);
                    if (existingUnitType == null)
                    {
                        return NotFound();
                    }

                    _context.Entry(existingUnitType).CurrentValues.SetValues(unitType);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "اطلاعات نوع واحد با موفقیت به‌روزرسانی شد";
                    return RedirectToAction(nameof(UnitTypeList));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UnitTypeExists(unitType.UnitTypeId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            return View(unitType);
        }

        // POST: UnitType/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var unitType = await _context.UnitTypes.FindAsync(id);
            if (unitType == null)
            {
                TempData["ErrorMessage"] = "نوع واحد مورد نظر یافت نشد";
                return RedirectToAction(nameof(UnitTypeList));
            }

            // Check if unit type has any associated units
            bool hasUnits = await _context.Units.AnyAsync(u => u.UnitTypeId == id);

            if (hasUnits)
            {
                TempData["ErrorMessage"] = "این نوع واحد دارای واحد وابسته است و قابل حذف نیست";
                return RedirectToAction(nameof(UnitTypeList));
            }

            // Check if it's a system unit type
            if (unitType.IsSystem == true)
            {
                TempData["ErrorMessage"] = "نوع واحد سیستمی قابل حذف نیست";
                return RedirectToAction(nameof(UnitTypeList));
            }

            try
            {
                _context.UnitTypes.Remove(unitType);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "نوع واحد با موفقیت حذف شد";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "خطا در حذف نوع واحد: " + ex.Message;
            }

            return RedirectToAction(nameof(UnitTypeList));
        }

        private bool UnitTypeExists(Guid id)
        {
            return _context.UnitTypes.Any(e => e.UnitTypeId == id);
        }
    }
}