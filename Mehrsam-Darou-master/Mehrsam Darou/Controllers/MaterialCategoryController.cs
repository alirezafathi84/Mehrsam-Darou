using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mehrsam_Darou.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using static Mehrsam_Darou.Helper.Helper;

namespace Mehrsam_Darou.Controllers
{
    public class MaterialCategoryController : BaseController
    {
        private readonly DarouAppContext _context;

        public MaterialCategoryController(DarouAppContext context) : base(context)
        {
            _context = context;
        }

        // GET: MaterialCategory/MaterialCategoryList
        public async Task<IActionResult> MaterialCategoryList(int? page, string searchKey)
        {
            var setting = await ReadSettingAsync(_context);
            int pageSize = Convert.ToInt32(setting.NumberPerPage ?? 10);
            int pageNumber = page ?? 1;

            IQueryable<MaterialCategory> query = _context.MaterialCategories;

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

            var paginatedList = new PaginatedList<MaterialCategory>(items, total, pageNumber, pageSize);

            return View(paginatedList);
        }

        // GET: MaterialCategory/AddMaterialCategory
        public IActionResult AddMaterialCategory()
        {
            return View(new MaterialCategory { IsActive = true });
        }

        // POST: MaterialCategory/AddMaterialCategory
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMaterialCategory(MaterialCategory materialCategory)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    if (await _context.MaterialCategories.AnyAsync(c => c.CategoryName == materialCategory.CategoryName))
                    {
                        TempData["ErrorMessage"] = "دسته بندی با این نام قبلاً ثبت شده است";
                        return View(materialCategory);
                    }

                    _context.Add(materialCategory);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "دسته بندی جدید با موفقیت ایجاد شد";
                    return RedirectToAction(nameof(MaterialCategoryList));
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "خطا در ایجاد دسته بندی: " + ex.Message;
                }
            }
            return View(materialCategory);
        }

        // GET: MaterialCategory/EditMaterialCategory/5
        public async Task<IActionResult> EditMaterialCategory(Guid id)
        {
            var materialCategory = await _context.MaterialCategories.FindAsync(id);
            if (materialCategory == null)
            {
                return NotFound();
            }
            return View(materialCategory);
        }

        // POST: MaterialCategory/EditMaterialCategory/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditMaterialCategory(Guid id, MaterialCategory materialCategory)
        {
            if (id != materialCategory.CategoryId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (await _context.MaterialCategories.AnyAsync(c =>
                        c.CategoryId != id &&
                        c.CategoryName == materialCategory.CategoryName))
                    {
                        TempData["ErrorMessage"] = "دسته بندی با این نام قبلاً ثبت شده است";
                        return View(materialCategory);
                    }

                    _context.Update(materialCategory);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "اطلاعات دسته بندی با موفقیت به‌روزرسانی شد";
                    return RedirectToAction(nameof(MaterialCategoryList));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MaterialCategoryExists(materialCategory.CategoryId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            return View(materialCategory);
        }

        // POST: MaterialCategory/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var materialCategory = await _context.MaterialCategories.FindAsync(id);
            if (materialCategory == null)
            {
                TempData["ErrorMessage"] = "دسته بندی مورد نظر یافت نشد";
                return RedirectToAction(nameof(MaterialCategoryList));
            }

            // Check if category is used by any materials
            bool isUsed = await _context.RawMaterials.AnyAsync(m => m.CategoryId == id);
            if (isUsed)
            {
                TempData["ErrorMessage"] = "این دسته بندی توسط مواد اولیه استفاده شده و قابل حذف نیست";
                return RedirectToAction(nameof(MaterialCategoryList));
            }

            try
            {
                _context.MaterialCategories.Remove(materialCategory);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "دسته بندی با موفقیت حذف شد";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "خطا در حذف دسته بندی: " + ex.Message;
            }

            return RedirectToAction(nameof(MaterialCategoryList));
        }

        private bool MaterialCategoryExists(Guid id)
        {
            return _context.MaterialCategories.Any(e => e.CategoryId == id);
        }
    }
}