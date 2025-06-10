using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mehrsam_Darou.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using static Mehrsam_Darou.Helper.Helper;

namespace Mehrsam_Darou.Controllers
{
    public class FormulaController : BaseController
    {
        private readonly DarouAppContext _context;

        public FormulaController(DarouAppContext context) : base(context)
        {
            _context = context;
        }

        // GET: Formula/FormulaList
        public async Task<IActionResult> FormulaList(int? page, string searchKey)
        {
            var setting = await ReadSettingAsync(_context);
            int pageSize = Convert.ToInt32(setting.NumberPerPage ?? 10);
            int pageNumber = page ?? 1;

            IQueryable<Formula> query = _context.Formulas;

            if (!string.IsNullOrWhiteSpace(searchKey))
            {
                query = query.Where(f => f.FormulaName.Contains(searchKey) ||
                                     f.FormulaCode.Contains(searchKey) ||
                                     f.FormulaType.Contains(searchKey) ||
                                     f.DosageForm.Contains(searchKey) ||
                                     f.PharmacologicalClass.Contains(searchKey))
                            .OrderBy(f => f.FormulaName);
            }

            int total = await query.CountAsync();
            var items = await query
                .OrderByDescending(f => f.CreatedDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var paginatedList = new PaginatedList<Formula>(items, total, pageNumber, pageSize);

            return View(paginatedList);
        }

        // GET: Formula/AddFormula
        public async Task<IActionResult> AddFormula()
        {
            ViewBag.Medicines = await _context.Medicines
                .Where(m => m.IsActive == true)
                .Select(m => new { m.MedicineId, m.BrandName })
                .ToListAsync();

            ViewBag.Units = await _context.Units
                .Where(u => u.IsActive == true)
                .Select(u => new { u.UnitId, u.UnitName, u.UnitSymbol })
                .ToListAsync();

            ViewBag.Materials = await _context.RawMaterials
                .Where(rm => rm.IsActive == true)
                .Select(rm => new { rm.MaterialId, rm.MaterialName, rm.MaterialCode })
                .ToListAsync();

            return View(new Formula
            {
                IsActive = true,
                CreatedDate = DateTime.Now,
                FormulaVersion = "1.0",
                Currency = "IRR",
                FormulaStatus = "پیش‌نویس",
                ReviewFrequencyMonths = 12
            });
        }

        // POST: Formula/AddFormula
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddFormula(Formula formula)
        {
            // Remove navigation property validation errors
            ModelState.Remove("TargetMedicine");
            ModelState.Remove("StrengthUnit");
            ModelState.Remove("BatchSizeUnit");
            ModelState.Remove("ParentFormula");

            if (ModelState.IsValid)
            {
                try
                {
                    if (await _context.Formulas.AnyAsync(f => f.FormulaCode == formula.FormulaCode && f.FormulaVersion == formula.FormulaVersion))
                    {
                        TempData["ErrorMessage"] = "فرمولی با این کد و نسخه قبلاً ثبت شده است";
                        await LoadViewBagData();
                        return View(formula);
                    }

                    formula.FormulaId = Guid.NewGuid();
                    formula.CreatedDate = DateTime.Now;

                    // Set next review date if review frequency is provided
                    if (formula.ReviewFrequencyMonths.HasValue)
                    {
                        formula.NextReviewDate = DateOnly.FromDateTime(DateTime.Now.AddMonths(formula.ReviewFrequencyMonths.Value));
                    }

                    _context.Add(formula);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "فرمول جدید با موفقیت ایجاد شد";
                    return RedirectToAction(nameof(FormulaList));
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "خطا در ایجاد فرمول: " + ex.Message;
                }
            }

            await LoadViewBagData();
            return View(formula);
        }

        // GET: Formula/EditFormula/5
        public async Task<IActionResult> EditFormula(Guid id)
        {
            var formula = await _context.Formulas.FindAsync(id);

            if (formula == null)
            {
                return NotFound();
            }

            await LoadViewBagData();
            return View(formula);
        }

        // POST: Formula/EditFormula/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditFormula(Guid id, Formula formula)
        {
            if (id != formula.FormulaId)
            {
                return NotFound();
            }

            // Remove navigation property validation errors
            ModelState.Remove("TargetMedicine");
            ModelState.Remove("StrengthUnit");
            ModelState.Remove("BatchSizeUnit");
            ModelState.Remove("ParentFormula");

            if (ModelState.IsValid)
            {
                try
                {
                    if (await _context.Formulas.AnyAsync(f =>
                        f.FormulaId != id &&
                        f.FormulaCode == formula.FormulaCode &&
                        f.FormulaVersion == formula.FormulaVersion))
                    {
                        TempData["ErrorMessage"] = "فرمولی با این کد و نسخه قبلاً ثبت شده است";
                        await LoadViewBagData();
                        return View(formula);
                    }

                    var existingFormula = await _context.Formulas.FindAsync(id);
                    if (existingFormula == null)
                    {
                        return NotFound();
                    }

                    // Keep the original creation date and creator
                    formula.CreatedDate = existingFormula.CreatedDate;
                    formula.CreatedBy = existingFormula.CreatedBy;
                    formula.LastModifiedDate = DateTime.Now;

                    // Set next review date if review frequency is provided
                    if (formula.ReviewFrequencyMonths.HasValue)
                    {
                        formula.NextReviewDate = DateOnly.FromDateTime(DateTime.Now.AddMonths(formula.ReviewFrequencyMonths.Value));
                    }

                    _context.Entry(existingFormula).CurrentValues.SetValues(formula);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "اطلاعات فرمول با موفقیت به‌روزرسانی شد";
                    return RedirectToAction(nameof(FormulaList));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!FormulaExists(formula.FormulaId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "خطا در به‌روزرسانی فرمول: " + ex.Message;
                }
            }

            await LoadViewBagData();
            return View(formula);
        }

        // POST: Formula/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var formula = await _context.Formulas.FindAsync(id);
            if (formula == null)
            {
                TempData["ErrorMessage"] = "فرمول مورد نظر یافت نشد";
                return RedirectToAction(nameof(FormulaList));
            }

            try
            {
                // Check if formula has any production orders or batches
                var hasProductionOrders = await _context.ProductionOrders.AnyAsync(po => po.MedicineId == formula.MedicineId);
                var hasFinishedGoodsBatches = await _context.FinishedGoodsBatches.AnyAsync(fgb => fgb.MedicineId == formula.MedicineId);

                if (hasProductionOrders || hasFinishedGoodsBatches)
                {
                    TempData["ErrorMessage"] = "این فرمول دارای سفارش تولید یا بچ محصول است و قابل حذف نیست";
                    return RedirectToAction(nameof(FormulaList));
                }

                // Check if it's a master formula or has child formulas
                if (formula.IsMasterFormula == true)
                {
                    var hasChildFormulas = await _context.Formulas.AnyAsync(f => f.ParentFormulaId == id);
                    if (hasChildFormulas)
                    {
                        TempData["ErrorMessage"] = "این فرمول اصلی دارای فرمول‌های فرعی است و قابل حذف نیست";
                        return RedirectToAction(nameof(FormulaList));
                    }
                }

                // Delete formula ingredients first (cascade delete should handle this, but being explicit)
                var ingredients = await _context.FormulaIngredients.Where(fi => fi.FormulaId == id).ToListAsync();
                if (ingredients.Any())
                {
                    _context.FormulaIngredients.RemoveRange(ingredients);
                }

                _context.Formulas.Remove(formula);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "فرمول با موفقیت حذف شد";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "خطا در حذف فرمول: " + ex.Message;
            }

            return RedirectToAction(nameof(FormulaList));
        }

        // GET: Formula/Details/5
        public async Task<IActionResult> Details(Guid id)
        {
            var formula = await _context.Formulas.FindAsync(id);

            if (formula == null)
            {
                return NotFound();
            }

            // Load ingredients
            var ingredients = await _context.FormulaIngredients
                .Where(fi => fi.FormulaId == id && fi.IsActive == true)
                .OrderBy(fi => fi.SequenceOrder)
                .ToListAsync();

            ViewBag.Ingredients = ingredients;

            return View(formula);
        }

        // GET: Formula/ManageIngredients/5
        public async Task<IActionResult> ManageIngredients(Guid id)
        {
            var formula = await _context.Formulas.FindAsync(id);
            if (formula == null)
            {
                return NotFound();
            }

            var ingredients = await _context.FormulaIngredients
                .Where(fi => fi.FormulaId == id)
                .OrderBy(fi => fi.SequenceOrder)
                .ToListAsync();

            ViewBag.Formula = formula;
            ViewBag.Materials = await _context.RawMaterials
                .Where(rm => rm.IsActive == true)
                .Select(rm => new { rm.MaterialId, rm.MaterialName, rm.MaterialCode })
                .ToListAsync();

            ViewBag.Units = await _context.Units
                .Where(u => u.IsActive == true)
                .Select(u => new { u.UnitId, u.UnitName, u.UnitSymbol })
                .ToListAsync();

            return View(ingredients);
        }

        // POST: Formula/AddIngredient
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddIngredient(Guid FormulaId, Guid MaterialId, string IngredientName,
            string FunctionType, decimal Quantity, Guid UnitId, decimal? Percentage, decimal? CostPerUnit,
            string SupplierPreference, int? SequenceOrder, string Specification, string AdditionMethod, string Notes)
        {
            try
            {
                // Basic validation
                if (string.IsNullOrWhiteSpace(IngredientName))
                {
                    TempData["ErrorMessage"] = "نام ماده الزامی است";
                    return RedirectToAction("ManageIngredients", new { id = FormulaId });
                }

                if (Quantity <= 0)
                {
                    TempData["ErrorMessage"] = "مقدار باید بیشتر از صفر باشد";
                    return RedirectToAction("ManageIngredients", new { id = FormulaId });
                }

                // Check if material already exists in this formula
                if (await _context.FormulaIngredients.AnyAsync(fi => fi.FormulaId == FormulaId && fi.MaterialId == MaterialId))
                {
                    TempData["ErrorMessage"] = "این ماده قبلاً به فرمول اضافه شده است";
                    return RedirectToAction("ManageIngredients", new { id = FormulaId });
                }

                var ingredient = new FormulaIngredient
                {
                    IngredientId = Guid.NewGuid(),
                    FormulaId = FormulaId,
                    MaterialId = MaterialId,
                    IngredientName = IngredientName,
                    FunctionType = FunctionType,
                    Quantity = Quantity,
                    UnitId = UnitId,
                    Percentage = Percentage,
                    CostPerUnit = CostPerUnit,
                    SupplierPreference = SupplierPreference,
                    SequenceOrder = SequenceOrder,
                    Specification = Specification,
                    AdditionMethod = AdditionMethod,
                    Notes = Notes,
                    CreatedDate = DateTime.Now,
                    IsActive = true
                };

                _context.FormulaIngredients.Add(ingredient);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "ماده با موفقیت به فرمول اضافه شد";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "خطا در افزودن ماده: " + ex.Message;
            }

            return RedirectToAction("ManageIngredients", new { id = FormulaId });
        }

        // POST: Formula/EditIngredient
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditIngredient(Guid IngredientId, Guid FormulaId, Guid MaterialId, string IngredientName,
            string FunctionType, decimal Quantity, Guid UnitId, decimal? Percentage, decimal? CostPerUnit,
            string SupplierPreference, int? SequenceOrder, string Specification, string AdditionMethod, string Notes)
        {
            try
            {
                // Basic validation
                if (string.IsNullOrWhiteSpace(IngredientName))
                {
                    TempData["ErrorMessage"] = "نام ماده الزامی است";
                    return RedirectToAction("ManageIngredients", new { id = FormulaId });
                }

                if (Quantity <= 0)
                {
                    TempData["ErrorMessage"] = "مقدار باید بیشتر از صفر باشد";
                    return RedirectToAction("ManageIngredients", new { id = FormulaId });
                }

                // Check if material already exists in this formula (excluding current ingredient)
                if (await _context.FormulaIngredients.AnyAsync(fi => fi.FormulaId == FormulaId && fi.MaterialId == MaterialId && fi.IngredientId != IngredientId))
                {
                    TempData["ErrorMessage"] = "این ماده قبلاً به فرمول اضافه شده است";
                    return RedirectToAction("ManageIngredients", new { id = FormulaId });
                }

                var existingIngredient = await _context.FormulaIngredients.FindAsync(IngredientId);
                if (existingIngredient == null)
                {
                    TempData["ErrorMessage"] = "ماده مورد نظr یافت نشد";
                    return RedirectToAction("ManageIngredients", new { id = FormulaId });
                }

                // Update the ingredient
                existingIngredient.MaterialId = MaterialId;
                existingIngredient.IngredientName = IngredientName;
                existingIngredient.FunctionType = FunctionType;
                existingIngredient.Quantity = Quantity;
                existingIngredient.UnitId = UnitId;
                existingIngredient.Percentage = Percentage;
                existingIngredient.CostPerUnit = CostPerUnit;
                existingIngredient.SupplierPreference = SupplierPreference;
                existingIngredient.SequenceOrder = SequenceOrder;
                existingIngredient.Specification = Specification;
                existingIngredient.AdditionMethod = AdditionMethod;
                existingIngredient.Notes = Notes;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "ماده با موفقیت به‌روزرسانی شد";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "خطا در به‌روزرسانی ماده: " + ex.Message;
            }

            return RedirectToAction("ManageIngredients", new { id = FormulaId });
        }

        // POST: Formula/DeleteIngredient/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteIngredient(Guid id, Guid formulaId)
        {
            try
            {
                var ingredient = await _context.FormulaIngredients.FindAsync(id);
                if (ingredient != null)
                {
                    _context.FormulaIngredients.Remove(ingredient);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "ماده با موفقیت حذف شد";
                }
                else
                {
                    TempData["ErrorMessage"] = "ماده مورد نظر یافت نشد";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "خطا در حذف ماده: " + ex.Message;
            }

            return RedirectToAction("ManageIngredients", new { id = formulaId });
        }

        private async Task LoadViewBagData()
        {
            ViewBag.Medicines = await _context.Medicines
                .Where(m => m.IsActive == true)
                .Select(m => new { m.MedicineId, m.BrandName })
                .ToListAsync();

            ViewBag.Units = await _context.Units
                .Where(u => u.IsActive == true)
                .Select(u => new { u.UnitId, u.UnitName, u.UnitSymbol })
                .ToListAsync();

            ViewBag.Materials = await _context.RawMaterials
                .Where(rm => rm.IsActive == true)
                .Select(rm => new { rm.MaterialId, rm.MaterialName, rm.MaterialCode })
                .ToListAsync();

            ViewBag.ParentFormulas = await _context.Formulas
                .Where(f => f.IsActive == true && f.IsMasterFormula == true)
                .Select(f => new { f.FormulaId, f.FormulaName, f.FormulaCode })
                .ToListAsync();
        }

        private bool FormulaExists(Guid id)
        {
            return _context.Formulas.Any(e => e.FormulaId == id);
        }
    }
}