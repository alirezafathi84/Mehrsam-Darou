using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mehrsam_Darou.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using static Mehrsam_Darou.Helper.Helper;

namespace Mehrsam_Darou.Controllers
{
    public class MaterialBatchController : BaseController
    {
        private readonly DarouAppContext _context;

        public MaterialBatchController(DarouAppContext context) : base(context)
        {
            _context = context;
        }

        // GET: MaterialBatch/MaterialBatchList
        public async Task<IActionResult> MaterialBatchList(int? page, string searchKey)
        {
            var setting = await ReadSettingAsync(_context);
            int pageSize = Convert.ToInt32(setting.NumberPerPage ?? 10);
            int pageNumber = page ?? 1;

            IQueryable<MaterialBatch> query = _context.MaterialBatches
                .Include(m => m.Material)
                .Include(m => m.Unit)
                .Include(m => m.Location);

            if (!string.IsNullOrWhiteSpace(searchKey))
            {
                query = query.Where(m => m.BatchNumber.Contains(searchKey) ||
                                     m.Material.MaterialName.Contains(searchKey) ||
                                     m.Material.MaterialCode.Contains(searchKey) ||
                                     m.Status.Contains(searchKey))
                            .OrderBy(m => m.Material.MaterialName);
            }

            int total = await query.CountAsync();
            var items = await query
                .OrderBy(m => m.Material.MaterialName)
                .ThenBy(m => m.BatchNumber)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var paginatedList = new PaginatedList<MaterialBatch>(items, total, pageNumber, pageSize);

            return View(paginatedList);
        }

        // GET: MaterialBatch/AddMaterialBatch
        public async Task<IActionResult> AddMaterialBatch()
        {
            ViewBag.Materials = await _context.RawMaterials
                .Where(m => m.IsActive == true)
                .OrderBy(m => m.MaterialName)
                .Select(m => new { m.MaterialId, m.MaterialName, m.MaterialCode })
                .ToListAsync();

            ViewBag.Units = await _context.Units
                .Where(u => u.IsActive == true)
                .OrderBy(u => u.UnitName)
                .Select(u => new { u.UnitId, u.UnitName, u.UnitSymbol })
                .ToListAsync();

            ViewBag.Locations = await _context.StorageLocations
                .Where(l => l.IsActive == true)
                .OrderBy(l => l.LocationName)
                .Select(l => new { l.LocationId, l.LocationName, l.LocationCode })
                .ToListAsync();

            return View(new MaterialBatch { Status = "قرنطینه" });
        }

        // POST: MaterialBatch/AddMaterialBatch
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMaterialBatch(MaterialBatch materialBatch)
        {
            // Remove navigation property validation errors
            ModelState.Remove("Material");
            ModelState.Remove("Unit");
            ModelState.Remove("Location");

            if (ModelState.IsValid)
            {
                try
                {
                    // Check if batch with same material and batch number already exists
                    if (await _context.MaterialBatches.AnyAsync(m =>
                        m.MaterialId == materialBatch.MaterialId &&
                        m.BatchNumber == materialBatch.BatchNumber))
                    {
                        TempData["ErrorMessage"] = "بچ با این شماره برای این ماده اولیه قبلاً ثبت شده است";
                        await LoadViewBagData();
                        return View(materialBatch);
                    }

                    materialBatch.BatchId = Guid.NewGuid();
                    materialBatch.CurrentQuantity = materialBatch.InitialQuantity;

                    _context.Add(materialBatch);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "بچ جدید با موفقیت ایجاد شد";
                    return RedirectToAction(nameof(MaterialBatchList));
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "خطا در ایجاد بچ: " + ex.Message;
                }
            }

            await LoadViewBagData();
            return View(materialBatch);
        }

        // GET: MaterialBatch/EditMaterialBatch/5
        public async Task<IActionResult> EditMaterialBatch(Guid id)
        {
            var materialBatch = await _context.MaterialBatches
                .Include(m => m.Material)
                .FirstOrDefaultAsync(m => m.BatchId == id);

            if (materialBatch == null)
            {
                return NotFound();
            }

            await LoadViewBagData();
            return View(materialBatch);
        }

        // POST: MaterialBatch/EditMaterialBatch/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditMaterialBatch(Guid id, MaterialBatch materialBatch)
        {
            if (id != materialBatch.BatchId)
            {
                return NotFound();
            }

            // Remove navigation property validation errors
            ModelState.Remove("Material");
            ModelState.Remove("Unit");
            ModelState.Remove("Location");

            if (ModelState.IsValid)
            {
                try
                {
                    // Check if batch with same material and batch number already exists (excluding current)
                    if (await _context.MaterialBatches.AnyAsync(m =>
                        m.BatchId != id &&
                        m.MaterialId == materialBatch.MaterialId &&
                        m.BatchNumber == materialBatch.BatchNumber))
                    {
                        TempData["ErrorMessage"] = "بچ با این شماره برای این ماده اولیه قبلاً ثبت شده است";
                        await LoadViewBagData();
                        return View(materialBatch);
                    }

                    var existingBatch = await _context.MaterialBatches.FindAsync(id);
                    if (existingBatch == null)
                    {
                        return NotFound();
                    }

                    // Update only allowed fields (don't change MaterialId and BatchNumber after creation)
                    existingBatch.InitialQuantity = materialBatch.InitialQuantity;
                    existingBatch.CurrentQuantity = materialBatch.CurrentQuantity;
                    existingBatch.UnitId = materialBatch.UnitId;
                    existingBatch.LocationId = materialBatch.LocationId;
                    existingBatch.Status = materialBatch.Status;
                    existingBatch.ExpiryDate = materialBatch.ExpiryDate;

                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "اطلاعات بچ با موفقیت به‌روزرسانی شد";
                    return RedirectToAction(nameof(MaterialBatchList));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MaterialBatchExists(materialBatch.BatchId))
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
                    TempData["ErrorMessage"] = "خطا در به‌روزرسانی بچ: " + ex.Message;
                }
            }

            await LoadViewBagData();
            return View(materialBatch);
        }

        // POST: MaterialBatch/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var materialBatch = await _context.MaterialBatches.FindAsync(id);
            if (materialBatch == null)
            {
                TempData["ErrorMessage"] = "بچ مورد نظر یافت نشد";
                return RedirectToAction(nameof(MaterialBatchList));
            }

            // Check if batch has been used in any purchase invoice items
            bool hasInvoiceItems = await _context.PurchaseInvoiceItems.AnyAsync(p => p.BatchId == id);

            if (hasInvoiceItems)
            {
                TempData["ErrorMessage"] = "این بچ در فاکتورهای خرید استفاده شده و قابل حذف نیست";
                return RedirectToAction(nameof(MaterialBatchList));
            }

            // Check if batch has current quantity > 0
            if (materialBatch.CurrentQuantity > 0)
            {
                TempData["ErrorMessage"] = "این بچ دارای موجودی است و قابل حذف نیست";
                return RedirectToAction(nameof(MaterialBatchList));
            }

            try
            {
                _context.MaterialBatches.Remove(materialBatch);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "بچ با موفقیت حذف شد";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "خطا در حذف بچ: " + ex.Message;
            }

            return RedirectToAction(nameof(MaterialBatchList));
        }

        private bool MaterialBatchExists(Guid id)
        {
            return _context.MaterialBatches.Any(e => e.BatchId == id);
        }

        private async Task LoadViewBagData()
        {
            ViewBag.Materials = await _context.RawMaterials
                .Where(m => m.IsActive == true)
                .OrderBy(m => m.MaterialName)
                .Select(m => new { m.MaterialId, m.MaterialName, m.MaterialCode })
                .ToListAsync();

            ViewBag.Units = await _context.Units
                .Where(u => u.IsActive == true)
                .OrderBy(u => u.UnitName)
                .Select(u => new { u.UnitId, u.UnitName, u.UnitSymbol })
                .ToListAsync();

            ViewBag.Locations = await _context.StorageLocations
                .Where(l => l.IsActive == true)
                .OrderBy(l => l.LocationName)
                .Select(l => new { l.LocationId, l.LocationName, l.LocationCode })
                .ToListAsync();
        }
    }
}