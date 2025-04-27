using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mehrsam_Darou.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using static Mehrsam_Darou.Helper.Helper;

namespace Mehrsam_Darou.Controllers
{
    public class MedicineCategoryController : BaseController
    {
        private readonly DarouAppContext _context;

        public MedicineCategoryController(DarouAppContext context) : base(context)
        {
            _context = context;
        }

        // GET: MedicineCategory
        public async Task<IActionResult> MedicineCategoryList(int? page, string searchKey)
        {
            var setting = await ReadSettingAsync(_context);
            int pageSize = Convert.ToInt32(setting.NumberPerPage ?? 10);
            int pageNumber = page ?? 1;

            IQueryable<MedicineCategory> query = _context.MedicineCategories;

            if (!string.IsNullOrWhiteSpace(searchKey))
            {
                query = query.Where(c => c.CategoryName.Contains(searchKey))
                            .OrderBy(c => c.CategoryName);
            }

            int total = await query.CountAsync();
            var items = await query
                .OrderBy(c => c.CategoryName)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var paginatedList = new PaginatedList<MedicineCategory>(items, total, pageNumber, pageSize);

            return View(paginatedList);
        }

        // GET: MedicineCategory/Create
        public IActionResult AddMedicineCategory()
        {
            return View(new MedicineCategory { IsActive = true });
        }

        // POST: MedicineCategory/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMedicineCategory(MedicineCategory medicineCategory)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Add(medicineCategory);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "دسته بندی جدید با موفقیت ایجاد شد";
                    return RedirectToAction(nameof(MedicineCategoryList));
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "خطا در ایجاد دسته بندی: " + ex.Message;
                }
            }
            return View(medicineCategory);
        }

        // GET: MedicineCategory/Edit/5
        public async Task<IActionResult> EditMedicineCategory(Guid id)
        {
            var medicineCategory = await _context.MedicineCategories.FindAsync(id);
            if (medicineCategory == null)
            {
                return NotFound();
            }
            return View(medicineCategory);
        }

        // POST: MedicineCategory/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditMedicineCategory(Guid id, MedicineCategory medicineCategory)
        {
            if (id != medicineCategory.CategoryId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(medicineCategory);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "اطلاعات دسته بندی با موفقیت به‌روزرسانی شد";
                    return RedirectToAction(nameof(MedicineCategoryList));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CategoryExists(medicineCategory.CategoryId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            return View(medicineCategory);
        }

        // POST: MedicineCategory/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var medicineCategory = await _context.MedicineCategories.FindAsync(id);
            if (medicineCategory == null)
            {
                TempData["ErrorMessage"] = "دسته بندی مورد نظر یافت نشد";
                return RedirectToAction(nameof(MedicineCategoryList));
            }

            // Check if category is used by any medicines
            bool isUsed = await _context.Medicines.AnyAsync(m => m.CategoryId == id);
            if (isUsed)
            {
                TempData["ErrorMessage"] = "این دسته بندی توسط داروها استفاده شده و قابل حذف نیست";
                return RedirectToAction(nameof(MedicineCategoryList));
            }

            try
            {
                _context.MedicineCategories.Remove(medicineCategory);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "دسته بندی با موفقیت حذف شد";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "خطا در حذف دسته بندی: " + ex.Message;
            }

            return RedirectToAction(nameof(MedicineCategoryList));
        }

        private bool CategoryExists(Guid id)
        {
            return _context.MedicineCategories.Any(e => e.CategoryId == id);
        }
    }
}