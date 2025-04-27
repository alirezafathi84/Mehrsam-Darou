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
    public class MedicineController : BaseController
    {
        private readonly DarouAppContext _context;

        public MedicineController(DarouAppContext context) : base(context)
        {
            _context = context;
        }

        public async Task<IActionResult> MedicineList(int? page, string SearchKey)
        {
            var setting = await ReadSettingAsync(_context);
            int pageSize = Convert.ToInt32(setting.NumberPerPage ?? 10);
            int pageNumber = page ?? 1;

            IQueryable<Medicine> query = _context.Medicines
                .Include(m => m.Category)      // Include Category navigation property!
                .Include(m => m.StrengthUnit); // Also include StrengthUnit as before

            if (!string.IsNullOrWhiteSpace(SearchKey))
            {
                query = query.Where(m =>
                    m.BrandName.Contains(SearchKey) ||
                    m.MedicineCode.Contains(SearchKey))
                    .OrderBy(m => m.BrandName);
            }

            int total = await query.CountAsync();

            var Items = await query
                .OrderBy(m => m.BrandName)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var paginatedMedicines = new PaginatedList<Medicine>(Items, total, pageNumber, pageSize);

            return View(paginatedMedicines);
        }

        public IActionResult AddMedicine()
        {
            var model = new Medicine
            {
                IsActive = true
            };

            PopulateDropdowns();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMedicine(Medicine medicine)
        {
            // Explicitly validate CategoryId
            if (medicine.CategoryId == Guid.Empty)
            {
                ModelState.AddModelError("CategoryId", "لطفاً دسته‌بندی را انتخاب کنید");
            }
            else { 

            medicine.Category = await _context.MedicineCategories
                        .FindAsync(medicine.CategoryId);
           
            }

          
                try
                {
                    // Attach the category to prevent validation errors
        

                    _context.Add(medicine);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "دارو با موفقیت اضافه شد";
                    return RedirectToAction(nameof(MedicineList));
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "خطا در ذخیره سازی: " + ex.Message;
                }
            

            // Repopulate dropdowns
            ViewBag.StrengthUnits = new SelectList(_context.Units.ToList(), "UnitId", "UnitName");
            ViewBag.Categories = new SelectList(
                _context.MedicineCategories.Where(c => c.IsActive == true).ToList(),
                "CategoryId",
                "CategoryName",
                medicine.CategoryId  // Preselect the previously selected value
            );

            return View(medicine);
        }

        private void PopulateDropdowns()
        {
            ViewBag.StrengthUnits = new SelectList(_context.Units.ToList(), "UnitId", "UnitName");
            ViewBag.Categories = new SelectList(_context.MedicineCategories.Where(c => c.IsActive == true).ToList(), "CategoryId", "CategoryName");
        }

        // GET: Medicine/Edit/5
        public async Task<IActionResult> EditMedicine(Guid id)
        {
            var medicine = await _context.Medicines.FindAsync(id);
            if (medicine == null)
            {
                return NotFound();
            }
            PopulateDropdowns();
            return View(medicine);
        }

        // POST: Medicine/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditMedicine(Guid id, Medicine medicine)
        {
            if (id != medicine.MedicineId)
            {
                return NotFound();
            }
         

          
                try
                {
                    // Optional: Set last modified date field if available, e.g.:
                    // medicine.LastModified = DateTime.Now;

                    _context.Update(medicine);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "اطلاعات دارو با موفقیت به‌روزرسانی شد";
                    return RedirectToAction(nameof(MedicineList));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MedicineExists(medicine.MedicineId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            

                PopulateDropdowns();

            TempData["ErrorMessage"] = "لطفاً اطلاعات را صحیح وارد نمایید";
            return View(medicine);
        }


        // POST: Medicine/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMedicine(Guid id)
        {
            var medicine = await _context.Medicines.FindAsync(id);
            if (medicine == null)
            {
                TempData["ErrorMessage"] = "دارو مورد نظر یافت نشد";
                return RedirectToAction(nameof(MedicineList));
            }

            try
            {
                _context.Medicines.Remove(medicine);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "دارو با موفقیت حذف شد";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "خطا در حذف دارو: " + ex.Message;
            }

            return RedirectToAction(nameof(MedicineList));
        }

        private bool MedicineExists(Guid id)
        {
            return _context.Medicines.Any(e => e.MedicineId == id);
        }

        // BOM

        public async Task<IActionResult> MedicineBOM(Guid id)
        {
            var medicine = await _context.Medicines
                .Include(m => m.MedicineBoms)
                    .ThenInclude(b => b.Material)
                .Include(m => m.MedicineBoms)
                    .ThenInclude(b => b.Unit)
                .FirstOrDefaultAsync(m => m.MedicineId == id);

            if (medicine == null)
            {
                return NotFound();
            }

            return View(medicine);
        }

        // Add these methods to your existing MedicineController

        [HttpGet]
        public async Task<IActionResult> AddBOM(Guid medicineId)
        {
            var medicine = await _context.Medicines.FindAsync(medicineId);
            if (medicine == null)
            {
                return NotFound();
            }

            ViewBag.MedicineName = medicine.BrandName;

            var model = new MedicineBom
            {
                MedicineId = medicineId,
                IsActive = true
            };

            await PopulateBOMDropdowns();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddBOM(MedicineBom medicineBom)
        {
           
                try
                {
                    // Check if this material already exists in BOM for this medicine
                    bool exists = await _context.MedicineBoms
                        .AnyAsync(b => b.MedicineId == medicineBom.MedicineId &&
                                      b.MaterialId == medicineBom.MaterialId);

                    if (exists)
                    {
                        TempData["ErrorMessage"] = "این ماده اولیه قبلاً به BOM این دارو اضافه شده است";
                        await PopulateBOMDropdowns();
                        return View(medicineBom);
                    }

                    _context.Add(medicineBom);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "ماده اولیه با موفقیت به BOM اضافه شد";
                    return RedirectToAction(nameof(MedicineBOM), new { id = medicineBom.MedicineId });
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "خطا در ذخیره سازی: " + ex.Message;
                }
            

            await PopulateBOMDropdowns();
            return View(medicineBom);
        }

        [HttpGet]
        public async Task<IActionResult> EditBOM(Guid id)
        {
            var bom = await _context.MedicineBoms
                .Include(b => b.Medicine)
                .Include(b => b.Material)
                .Include(b => b.Unit)
                .FirstOrDefaultAsync(b => b.BomId == id);

            if (bom == null)
            {
                return NotFound();
            }

            ViewBag.MedicineName = bom.Medicine?.BrandName;
            ViewBag.MaterialName = bom.Material?.MaterialName;

            await PopulateBOMDropdowns();
            return View(bom);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditBOM(Guid id, MedicineBom medicineBom)
        {
            if (id != medicineBom.BomId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(medicineBom);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "اطلاعات BOM با موفقیت به‌روزرسانی شد";
                    return RedirectToAction(nameof(MedicineBOM), new { id = medicineBom.MedicineId });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BOMExists(medicineBom.BomId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            await PopulateBOMDropdowns();
            return View(medicineBom);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteBOM(Guid id)
        {
            var bom = await _context.MedicineBoms
                .Include(b => b.Medicine)
                .FirstOrDefaultAsync(b => b.BomId == id);

            if (bom == null)
            {
                TempData["ErrorMessage"] = "رکورد BOM مورد نظر یافت نشد";
                return RedirectToAction(nameof(MedicineList));
            }

            try
            {
                _context.MedicineBoms.Remove(bom);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "رکورد BOM با موفقیت حذف شد";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "خطا در حذف رکورد BOM: " + ex.Message;
            }

            return RedirectToAction(nameof(MedicineBOM), new { id = bom.MedicineId });
        }

        private bool BOMExists(Guid id)
        {
            return _context.MedicineBoms.Any(e => e.BomId == id);
        }

        private async Task PopulateBOMDropdowns()
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
        }











    }
}