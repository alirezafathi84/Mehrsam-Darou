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
            // Set common view data and get the page size
            var setting = await ReadSettingAsync(_context);
            int pageSize = Convert.ToInt32(setting.NumberPerPage ?? 10); // Default to 10 if setting.NumberPerPage is null
            int pageNumber = page ?? 1;

            // Base query for fetching medicines
            IQueryable<Medicine> query = _context.Medicines;

            // Apply search filter if SearchKey is provided
            if (!string.IsNullOrWhiteSpace(SearchKey))
            {
                query = query.Where(m =>
                    m.BrandName.Contains(SearchKey) ||
                    m.MedicineCode.Contains(SearchKey))
                    .OrderBy(m => m.BrandName);
            }

            // Get total count after filtering
            int total = await query.CountAsync();

            // Fetch paginated results
            var Items = await query
                .OrderBy(m => m.BrandName)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Create the paginated list
            var paginatedMedicines = new PaginatedList<Medicine>(Items, total, pageNumber, pageSize);

            // Pass paginated list to the view
            return View(paginatedMedicines);
        }

        public IActionResult AddMedicine()
        {
            var model = new Medicine
            {
                IsActive = true // مقدار پیش فرض
            };

            ViewBag.StrengthUnits = new SelectList(_context.Units.ToList(), "UnitId", "UnitName");
            ViewBag.Categories = new SelectList(_context.MedicineCategories.Where(c => c.IsActive == true).ToList(), "CategoryId", "CategoryName");

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMedicine(Medicine medicine)
        {
            if (ModelState.IsValid)
            {
                medicine.MedicineId = Guid.NewGuid();
                _context.Add(medicine);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "دارو با موفقیت اضافه شد";
                return RedirectToAction(nameof(MedicineList));
            }

            TempData["ErrorMessage"] = "لطفاً اطلاعات را صحیح وارد نمایید";
            ViewBag.StrengthUnits = new SelectList(_context.Units.ToList(), "UnitId", "UnitName");
            ViewBag.Categories = new SelectList(_context.MedicineCategories.Where(c => c.IsActive == true).ToList(), "CategoryId", "CategoryName");

            return View(medicine);
        }

        // GET: Medicine/Edit/5
        public async Task<IActionResult> EditMedicine(Guid id)
        {
            var medicine = await _context.Medicines.FindAsync(id);
            if (medicine == null)
            {
                return NotFound();
            }
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

            if (ModelState.IsValid)
            {
                try
                {
                    //medicine. = DateTime.Now;

                    _context.Update(medicine);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "اطلاعات دارو با موفقیت به‌روزرسانی شد";
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
                return RedirectToAction(nameof(MedicineList));
            }

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
    }
}