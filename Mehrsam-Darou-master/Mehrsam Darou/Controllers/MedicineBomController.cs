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
    public class MedicineBomController : BaseController
    {
        private readonly DarouAppContext _context;

        public MedicineBomController(DarouAppContext context) : base(context)
        {
            _context = context;
        }

        // GET: MedicineBom/MedicineBomList
        public async Task<IActionResult> MedicineBomList(int? page, string searchKey, Guid? medicineId)
        {
            var setting = await ReadSettingAsync(_context);
            int pageSize = Convert.ToInt32(setting.NumberPerPage ?? 10);
            int pageNumber = page ?? 1;

            IQueryable<MedicineBom> query = _context.MedicineBoms
                .Include(m => m.Medicine)
                .Include(m => m.Material)
                .Include(m => m.Unit);

            if (medicineId.HasValue)
            {
                query = query.Where(m => m.MedicineId == medicineId.Value);
            }

            if (!string.IsNullOrWhiteSpace(searchKey))
            {
                query = query.Where(m => m.Medicine.BrandName.Contains(searchKey) ||
                                     m.Medicine.MedicineCode.Contains(searchKey) ||
                                     m.Material.MaterialName.Contains(searchKey) ||
                                     m.Material.MaterialCode.Contains(searchKey));
            }

            query = query.OrderBy(m => m.Medicine.BrandName).ThenBy(m => m.Material.MaterialName);

            int total = await query.CountAsync();
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var paginatedList = new PaginatedList<MedicineBom>(items, total, pageNumber, pageSize);

            // For dropdown filter
            ViewBag.Medicines = await _context.Medicines
                .Where(m => m.IsActive == true)
                .OrderBy(m => m.BrandName)
                .Select(m => new SelectListItem
                {
                    Value = m.MedicineId.ToString(),
                    Text = $"{m.BrandName} ({m.MedicineCode})"
                })
                .ToListAsync();

            ViewBag.SelectedMedicineId = medicineId;

            return View(paginatedList);
        }

        // GET: MedicineBom/AddMedicineBom
        public async Task<IActionResult> AddMedicineBom()
        {
            await LoadDropdownData();
            return View(new MedicineBom { IsActive = true });
        }

        // POST: MedicineBom/AddMedicineBom
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMedicineBom(MedicineBom medicineBom)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    if (await _context.MedicineBoms.AnyAsync(m =>
                        m.MedicineId == medicineBom.MedicineId &&
                        m.MaterialId == medicineBom.MaterialId))
                    {
                        TempData["ErrorMessage"] = "این ماده اولیه قبلاً برای این دارو ثبت شده است";
                        await LoadDropdownData();
                        return View(medicineBom);
                    }

                    medicineBom.BomId = Guid.NewGuid();
                    _context.Add(medicineBom);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "فرمول دارو با موفقیت ایجاد شد";
                    return RedirectToAction(nameof(MedicineBomList));
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "خطا در ایجاد فرمول دارو: " + ex.Message;
                }
            }

            await LoadDropdownData();
            return View(medicineBom);
        }

        // GET: MedicineBom/EditMedicineBom/5
        public async Task<IActionResult> EditMedicineBom(Guid id)
        {
            var medicineBom = await _context.MedicineBoms
                .Include(m => m.Medicine)
                .Include(m => m.Material)
                .Include(m => m.Unit)
                .FirstOrDefaultAsync(m => m.BomId == id);

            if (medicineBom == null)
            {
                return NotFound();
            }

            await LoadDropdownData();
            return View(medicineBom);
        }

        // POST: MedicineBom/EditMedicineBom/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditMedicineBom(Guid id, MedicineBom medicineBom)
        {
            if (id != medicineBom.BomId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (await _context.MedicineBoms.AnyAsync(m =>
                        m.BomId != id &&
                        m.MedicineId == medicineBom.MedicineId &&
                        m.MaterialId == medicineBom.MaterialId))
                    {
                        TempData["ErrorMessage"] = "این ماده اولیه قبلاً برای این دارو ثبت شده است";
                        await LoadDropdownData();
                        return View(medicineBom);
                    }

                    _context.Update(medicineBom);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "فرمول دارو با موفقیت به‌روزرسانی شد";
                    return RedirectToAction(nameof(MedicineBomList));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MedicineBomExists(medicineBom.BomId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            await LoadDropdownData();
            return View(medicineBom);
        }

        // POST: MedicineBom/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var medicineBom = await _context.MedicineBoms.FindAsync(id);
            if (medicineBom == null)
            {
                TempData["ErrorMessage"] = "فرمول دارو مورد نظر یافت نشد";
                return RedirectToAction(nameof(MedicineBomList));
            }

            try
            {
                _context.MedicineBoms.Remove(medicineBom);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "فرمول دارو با موفقیت حذف شد";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "خطا در حذف فرمول دارو: " + ex.Message;
            }

            return RedirectToAction(nameof(MedicineBomList));
        }

        // GET: MedicineBom/GetMedicineFormula/5
        public async Task<IActionResult> GetMedicineFormula(Guid medicineId)
        {
            var formula = await _context.MedicineBoms
                .Include(m => m.Medicine)
                .Include(m => m.Material)
                .Include(m => m.Unit)
                .Where(m => m.MedicineId == medicineId && m.IsActive == true)
                .OrderBy(m => m.Material.MaterialName)
                .ToListAsync();

            return Json(formula.Select(f => new
            {
                materialName = f.Material.MaterialName,
                materialCode = f.Material.MaterialCode,
                quantity = f.Quantity,
                unitSymbol = f.Unit.UnitSymbol,
                unitName = f.Unit.UnitName
            }));
        }

        private async Task LoadDropdownData()
        {
            ViewBag.Medicines = await _context.Medicines
                .Where(m => m.IsActive == true)
                .OrderBy(m => m.BrandName)
                .Select(m => new SelectListItem
                {
                    Value = m.MedicineId.ToString(),
                    Text = $"{m.BrandName} ({m.MedicineCode})"
                })
                .ToListAsync();

            ViewBag.Materials = await _context.RawMaterials
                .Where(r => r.IsActive == true)
                .OrderBy(r => r.MaterialName)
                .Select(r => new SelectListItem
                {
                    Value = r.MaterialId.ToString(),
                    Text = $"{r.MaterialName} ({r.MaterialCode})"
                })
                .ToListAsync();

            ViewBag.Units = await _context.Units
                .Where(u => u.IsActive == true)
                .OrderBy(u => u.UnitName)
                .Select(u => new SelectListItem
                {
                    Value = u.UnitId.ToString(),
                    Text = $"{u.UnitName} ({u.UnitSymbol})"
                })
                .ToListAsync();
        }

        private bool MedicineBomExists(Guid id)
        {
            return _context.MedicineBoms.Any(e => e.BomId == id);
        }
    }
}