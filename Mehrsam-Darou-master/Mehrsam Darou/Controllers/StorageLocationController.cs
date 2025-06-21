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
    public class StorageLocationController : BaseController
    {
        private readonly DarouAppContext _context;

        public StorageLocationController(DarouAppContext context) : base(context)
        {
            _context = context;
        }

        // GET: StorageLocation/StorageLocationList
        public async Task<IActionResult> StorageLocationList(int? page, string searchKey)
        {
            var setting = await ReadSettingAsync(_context);
            int pageSize = Convert.ToInt32(setting.NumberPerPage ?? 10);
            int pageNumber = page ?? 1;

            IQueryable<StorageLocation> query = _context.StorageLocations
                .Include(s => s.CapacityUnit);

            if (!string.IsNullOrWhiteSpace(searchKey))
            {
                query = query.Where(s => s.LocationName.Contains(searchKey) ||
                                     s.LocationCode.Contains(searchKey))
                            .OrderBy(s => s.LocationName);
            }

            int total = await query.CountAsync();
            var items = await query
                .OrderBy(s => s.LocationName)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var paginatedList = new PaginatedList<StorageLocation>(items, total, pageNumber, pageSize);

            return View(paginatedList);
        }

        // GET: StorageLocation/AddStorageLocation
        public async Task<IActionResult> AddStorageLocation()
        {
            await PopulateStorageLocationDropdowns();
            return View(new StorageLocation
            {
                LocationId = Guid.NewGuid(),
                IsActive = true
            });
        }

        // POST: StorageLocation/AddStorageLocation
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddStorageLocation(StorageLocation storageLocation)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Check for duplicate location code
                    if (await _context.StorageLocations.AnyAsync(s =>
                        s.LocationCode == storageLocation.LocationCode))
                    {
                        TempData["ErrorMessage"] = "مکان انبار با این کد قبلاً ثبت شده است";
                        await PopulateStorageLocationDropdowns();
                        return View(storageLocation);
                    }

                    // Ensure we have a valid GUID
                    if (storageLocation.LocationId == Guid.Empty)
                    {
                        storageLocation.LocationId = Guid.NewGuid();
                    }

                    // Handle nullable CapacityUnitId
                    if (storageLocation.CapacityUnitId.HasValue && storageLocation.CapacityUnitId.Value == Guid.Empty)
                    {
                        storageLocation.CapacityUnitId = null;
                    }

                    // Set default values
                    if (storageLocation.IsActive == null)
                    {
                        storageLocation.IsActive = true;
                    }

                    _context.Add(storageLocation);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "مکان انبار جدید با موفقیت ایجاد شد";
                    return RedirectToAction(nameof(StorageLocationList));
                }
                catch (Exception ex)
                {
                    var innerMessage = ex.InnerException?.Message ?? "No inner exception";
                    TempData["ErrorMessage"] = $"خطا در ایجاد مکان انبار: {ex.Message}. جزئیات: {innerMessage}";
                }
            }
            else
            {
                // Show validation errors
                var errors = string.Join(", ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));
                TempData["ErrorMessage"] = $"خطاهای اعتبارسنجی: {errors}";
            }

            await PopulateStorageLocationDropdowns();
            return View(storageLocation);
        }

        // GET: StorageLocation/EditStorageLocation/5
        public async Task<IActionResult> EditStorageLocation(Guid id)
        {
            if (id == Guid.Empty)
            {
                TempData["ErrorMessage"] = "شناسه مکان انبار نامعتبر است";
                return RedirectToAction(nameof(StorageLocationList));
            }

            var storageLocation = await _context.StorageLocations.FindAsync(id);
            if (storageLocation == null)
            {
                TempData["ErrorMessage"] = "مکان انبار مورد نظر یافت نشد";
                return RedirectToAction(nameof(StorageLocationList));
            }

            await PopulateStorageLocationDropdowns();
            return View(storageLocation);
        }

        // POST: StorageLocation/EditStorageLocation/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditStorageLocation(Guid id, StorageLocation storageLocation)
        {
            if (id == Guid.Empty)
            {
                TempData["ErrorMessage"] = "شناسه مکان انبار نامعتبر است";
                return RedirectToAction(nameof(StorageLocationList));
            }

            if (id != storageLocation.LocationId)
            {
                TempData["ErrorMessage"] = "خطا در شناسایی مکان انبار";
                return RedirectToAction(nameof(StorageLocationList));
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Check for duplicate location code (excluding current record)
                    if (await _context.StorageLocations.AnyAsync(s =>
                        s.LocationId != id &&
                        s.LocationCode == storageLocation.LocationCode))
                    {
                        TempData["ErrorMessage"] = "مکان انبار با این کد قبلاً ثبت شده است";
                        await PopulateStorageLocationDropdowns();
                        return View(storageLocation);
                    }

                    // Handle nullable CapacityUnitId
                    if (storageLocation.CapacityUnitId.HasValue && storageLocation.CapacityUnitId.Value == Guid.Empty)
                    {
                        storageLocation.CapacityUnitId = null;
                    }

                    _context.Update(storageLocation);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "اطلاعات مکان انبار با موفقیت به‌روزرسانی شد";
                    return RedirectToAction(nameof(StorageLocationList));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!StorageLocationExists(storageLocation.LocationId))
                    {
                        TempData["ErrorMessage"] = "مکان انبار مورد نظر یافت نشد";
                        return RedirectToAction(nameof(StorageLocationList));
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    var innerMessage = ex.InnerException?.Message ?? "No inner exception";
                    TempData["ErrorMessage"] = $"خطا در به‌روزرسانی مکان انبار: {ex.Message}. جزئیات: {innerMessage}";
                }
            }
            else
            {
                // Show validation errors
                var errors = string.Join(", ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));
                TempData["ErrorMessage"] = $"خطاهای اعتبارسنجی: {errors}";
            }

            await PopulateStorageLocationDropdowns();
            return View(storageLocation);
        }

        // POST: StorageLocation/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var storageLocation = await _context.StorageLocations.FindAsync(id);
            if (storageLocation == null)
            {
                TempData["ErrorMessage"] = "مکان انبار مورد نظر یافت نشد";
                return RedirectToAction(nameof(StorageLocationList));
            }

            // Check if location is used by any batches
            bool isUsed = await _context.MaterialBatches.AnyAsync(m => m.LocationId == id) ||
                          await _context.FinishedGoodsBatches.AnyAsync(f => f.LocationId == id);

            if (isUsed)
            {
                TempData["ErrorMessage"] = "این مکان انبار توسط دسته‌های مواد یا محصولات استفاده شده و قابل حذف نیست";
                return RedirectToAction(nameof(StorageLocationList));
            }

            try
            {
                _context.StorageLocations.Remove(storageLocation);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "مکان انبار با موفقیت حذف شد";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "خطا در حذف مکان انبار: " + ex.Message;
            }

            return RedirectToAction(nameof(StorageLocationList));
        }

        private bool StorageLocationExists(Guid id)
        {
            return _context.StorageLocations.Any(e => e.LocationId == id);
        }

        private async Task PopulateStorageLocationDropdowns()
        {
            // Units for capacity
            var units = await _context.Units
                .Where(u => u.IsActive == true)
                .OrderBy(u => u.UnitName)
                .ToListAsync();

            ViewBag.CapacityUnits = new SelectList(units, "UnitId", "UnitName");

            // Location types - based on your database constraints
            var locationTypes = new List<SelectListItem>
            {
                new SelectListItem { Value = "انبار", Text = "انبار" },
                new SelectListItem { Value = "سردخانه", Text = "سردخانه" },
                new SelectListItem { Value = "قرنطینه", Text = "قرنطینه" },
                new SelectListItem { Value = "خطرناک", Text = "مواد خطرناک" }
            };

            ViewBag.LocationTypes = new SelectList(locationTypes, "Value", "Text");
        }
    }
}