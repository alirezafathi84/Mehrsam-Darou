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
            // Remove validation for navigation properties
            ModelState.Remove("Medicine");
            ModelState.Remove("Material");
            ModelState.Remove("Unit");

            // Remove any sub-properties of navigation properties
            var keysToRemove = ModelState.Keys.Where(k =>
                k.StartsWith("Medicine.") ||
                k.StartsWith("Material.") ||
                k.StartsWith("Unit.")).ToList();

            foreach (var key in keysToRemove)
            {
                ModelState.Remove(key);
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Check for duplicate combination
                    if (await _context.MedicineBoms.AnyAsync(m =>
                        m.MedicineId == medicineBom.MedicineId &&
                        m.MaterialId == medicineBom.MaterialId))
                    {
                        TempData["ErrorMessage"] = "این ماده اولیه قبلاً برای این دارو ثبت شده است";
                        await LoadDropdownData();
                        return View(medicineBom);
                    }

                    medicineBom.BomId = Guid.NewGuid();

                    // Set navigation properties to null to avoid EF issues
                    medicineBom.Medicine = null;
                    medicineBom.Material = null;
                    medicineBom.Unit = null;

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
                TempData["ErrorMessage"] = "فرمول دارو مورد نظر یافت نشد";
                return RedirectToAction(nameof(MedicineBomList));
            }

            await LoadDropdownData();
            return View(medicineBom);
        }

        // POST: MedicineBom/EditMedicineBom/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditMedicineBom(Guid id, MedicineBom medicineBom)
        {
            // If the route id is empty, try to get it from the model
            if (id == Guid.Empty && medicineBom.BomId != Guid.Empty)
            {
                id = medicineBom.BomId;
            }

            // Check if we have a valid ID
            if (id == Guid.Empty || medicineBom.BomId == Guid.Empty)
            {
                TempData["ErrorMessage"] = "شناسه فرمول دارو معتبر نیست";
                return RedirectToAction(nameof(MedicineBomList));
            }

            if (id != medicineBom.BomId)
            {
                TempData["ErrorMessage"] = "عدم تطابق شناسه فرمول دارو";
                return RedirectToAction(nameof(MedicineBomList));
            }

            // Remove validation errors for navigation properties that we don't want to validate
            ModelState.Remove("Medicine");
            ModelState.Remove("Material");
            ModelState.Remove("Unit");

            // Also remove any sub-properties of navigation properties
            var keysToRemove = ModelState.Keys.Where(k =>
                k.StartsWith("Medicine.") ||
                k.StartsWith("Material.") ||
                k.StartsWith("Unit.")).ToList();

            foreach (var key in keysToRemove)
            {
                ModelState.Remove(key);
            }

            // Validate required fields manually
            if (medicineBom.Quantity <= 0)
            {
                ModelState.AddModelError("Quantity", "مقدار باید بیشتر از صفر باشد");
            }

            if (medicineBom.UnitId == Guid.Empty)
            {
                ModelState.AddModelError("UnitId", "انتخاب واحد الزامی است");
            }

            if (medicineBom.MedicineId == Guid.Empty)
            {
                ModelState.AddModelError("MedicineId", "شناسه دارو معتبر نیست");
            }

            if (medicineBom.MaterialId == Guid.Empty)
            {
                ModelState.AddModelError("MaterialId", "شناسه ماده اولیه معتبر نیست");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Get the existing entity from database
                    var existingBom = await _context.MedicineBoms.FindAsync(medicineBom.BomId);

                    if (existingBom == null)
                    {
                        TempData["ErrorMessage"] = "فرمول دارو مورد نظر یافت نشد";
                        return RedirectToAction(nameof(MedicineBomList));
                    }

                    // Validate unit exists
                    if (!await _context.Units.AnyAsync(u => u.UnitId == medicineBom.UnitId && u.IsActive == true))
                    {
                        TempData["ErrorMessage"] = "واحد انتخاب شده معتبر نیست";
                        var reloadBom = await _context.MedicineBoms
                            .Include(m => m.Medicine)
                            .Include(m => m.Material)
                            .Include(m => m.Unit)
                            .FirstOrDefaultAsync(m => m.BomId == medicineBom.BomId);
                        await LoadDropdownData();
                        return View(reloadBom);
                    }

                    // Ensure that MedicineId and MaterialId haven't been tampered with
                    if (existingBom.MedicineId != medicineBom.MedicineId || existingBom.MaterialId != medicineBom.MaterialId)
                    {
                        TempData["ErrorMessage"] = "امکان تغییر دارو یا ماده اولیه وجود ندارد";
                        var reloadBom = await _context.MedicineBoms
                            .Include(m => m.Medicine)
                            .Include(m => m.Material)
                            .Include(m => m.Unit)
                            .FirstOrDefaultAsync(m => m.BomId == medicineBom.BomId);
                        await LoadDropdownData();
                        return View(reloadBom);
                    }

                    // Update only the allowed fields
                    existingBom.Quantity = medicineBom.Quantity;
                    existingBom.UnitId = medicineBom.UnitId;
                    existingBom.IsActive = medicineBom.IsActive;

                    // Save changes
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "فرمول دارو با موفقیت به‌روزرسانی شد";
                    return RedirectToAction(nameof(MedicineBomList));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MedicineBomExists(medicineBom.BomId))
                    {
                        TempData["ErrorMessage"] = "فرمول دارو مورد نظر یافت نشد";
                        return RedirectToAction(nameof(MedicineBomList));
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "خطا در به‌روزرسانی: فرمول توسط کاربر دیگری تغییر یافته است";
                    }
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "خطا در به‌روزرسانی فرمول دارو: " + ex.Message;
                }
            }

            // If we get here, something went wrong, reload the form
            var bomToReload = await _context.MedicineBoms
                .Include(m => m.Medicine)
                .Include(m => m.Material)
                .Include(m => m.Unit)
                .FirstOrDefaultAsync(m => m.BomId == medicineBom.BomId);

            if (bomToReload == null)
            {
                TempData["ErrorMessage"] = "فرمول دارو مورد نظر یافت نشد";
                return RedirectToAction(nameof(MedicineBomList));
            }

            await LoadDropdownData();
            return View(bomToReload);
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
            try
            {
                var formula = await _context.MedicineBoms
                    .Include(m => m.Medicine)
                    .Include(m => m.Material)
                    .Include(m => m.Unit)
                    .Where(m => m.MedicineId == medicineId && m.IsActive == true)
                    .OrderBy(m => m.Material.MaterialName)
                    .ToListAsync();

                var result = formula.Select(f => new
                {
                    materialName = f.Material?.MaterialName ?? "نامشخص",
                    materialCode = f.Material?.MaterialCode ?? "نامشخص",
                    quantity = f.Quantity,
                    unitSymbol = f.Unit?.UnitSymbol ?? "",
                    unitName = f.Unit?.UnitName ?? "نامشخص"
                });

                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new { error = "خطا در بارگذاری اطلاعات: " + ex.Message });
            }
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