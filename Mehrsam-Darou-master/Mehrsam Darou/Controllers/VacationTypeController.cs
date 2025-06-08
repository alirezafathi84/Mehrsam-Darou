using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mehrsam_Darou.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using static Mehrsam_Darou.Helper.Helper;

namespace Mehrsam_Darou.Controllers
{
    public class VacationTypeController : BaseController
    {
        private readonly DarouAppContext _context;

        public VacationTypeController(DarouAppContext context) : base(context)
        {
            _context = context;
        }

        // GET: VacationType/VacationTypeList
        public async Task<IActionResult> VacationTypeList(int? page, string searchKey)
        {
            var setting = await ReadSettingAsync(_context);
            int pageSize = Convert.ToInt32(setting.NumberPerPage ?? 10);
            int pageNumber = page ?? 1;

            IQueryable<VacationType> query = _context.VacationTypes;

            if (!string.IsNullOrWhiteSpace(searchKey))
            {
                query = query.Where(vt => vt.Name.Contains(searchKey) ||
                                     vt.Description.Contains(searchKey))
                            .OrderBy(vt => vt.Name);
            }

            int total = await query.CountAsync();
            var items = await query
                .OrderBy(vt => vt.Name)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var paginatedList = new PaginatedList<VacationType>(items, total, pageNumber, pageSize);

            return View(paginatedList);
        }

        // GET: VacationType/AddVacationType
        public IActionResult AddVacationType()
        {
            return View(new VacationType
            {
                IsPaid = true,
                MaxDaysPerYear = 30
            });
        }

        // POST: VacationType/AddVacationType
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddVacationType(VacationType vacationType)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Check if vacation type name already exists
                    if (await _context.VacationTypes.AnyAsync(vt => vt.Name == vacationType.Name))
                    {
                        TempData["ErrorMessage"] = "نوع مرخصی با این نام قبلاً ثبت شده است";
                        return View(vacationType);
                    }

                    vacationType.Id = Guid.NewGuid();

                    _context.Add(vacationType);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "نوع مرخصی جدید با موفقیت ایجاد شد";
                    return RedirectToAction(nameof(VacationTypeList));
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "خطا در ایجاد نوع مرخصی: " + ex.Message;
                }
            }

            return View(vacationType);
        }

        // GET: VacationType/EditVacationType/5
        public async Task<IActionResult> EditVacationType(Guid id)
        {
            var vacationType = await _context.VacationTypes.FindAsync(id);
            if (vacationType == null)
            {
                return NotFound();
            }

            return View(vacationType);
        }

        // POST: VacationType/EditVacationType/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditVacationType(Guid id, VacationType vacationType)
        {
            if (id != vacationType.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Check if vacation type name already exists (excluding current record)
                    if (await _context.VacationTypes.AnyAsync(vt =>
                        vt.Id != id &&
                        vt.Name == vacationType.Name))
                    {
                        TempData["ErrorMessage"] = "نوع مرخصی با این نام قبلاً ثبت شده است";
                        return View(vacationType);
                    }

                    var existingVacationType = await _context.VacationTypes.FindAsync(id);
                    if (existingVacationType == null)
                    {
                        return NotFound();
                    }

                    _context.Entry(existingVacationType).CurrentValues.SetValues(vacationType);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "نوع مرخصی با موفقیت به‌روزرسانی شد";
                    return RedirectToAction(nameof(VacationTypeList));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!VacationTypeExists(vacationType.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            return View(vacationType);
        }

        // POST: VacationType/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var vacationType = await _context.VacationTypes.FindAsync(id);
            if (vacationType == null)
            {
                TempData["ErrorMessage"] = "نوع مرخصی مورد نظر یافت نشد";
                return RedirectToAction(nameof(VacationTypeList));
            }

            // Check if vacation type is being used in any vacations
            bool hasVacations = await _context.Vacations.AnyAsync(v => v.TypeId == id);

            if (hasVacations)
            {
                TempData["ErrorMessage"] = "این نوع مرخصی در درخواست‌های مرخصی استفاده شده و قابل حذف نیست";
                return RedirectToAction(nameof(VacationTypeList));
            }

            try
            {
                _context.VacationTypes.Remove(vacationType);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "نوع مرخصی با موفقیت حذف شد";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "خطا در حذف نوع مرخصی: " + ex.Message;
            }

            return RedirectToAction(nameof(VacationTypeList));
        }

        // GET: VacationType/VacationTypeDetails/5
        public async Task<IActionResult> VacationTypeDetails(Guid id)
        {
            var vacationType = await _context.VacationTypes
                .Include(vt => vt.Vacations)
                    .ThenInclude(v => v.User)
                .FirstOrDefaultAsync(vt => vt.Id == id);

            if (vacationType == null)
            {
                return NotFound();
            }

            // Get usage statistics
            var usageStats = new
            {
                TotalVacations = vacationType.Vacations.Count,
                ApprovedVacations = vacationType.Vacations.Count(v => v.Status == "Approved"),
                PendingVacations = vacationType.Vacations.Count(v => v.Status == "Pending"),
                RejectedVacations = vacationType.Vacations.Count(v => v.Status == "Rejected"),
                TotalDaysUsed = vacationType.Vacations
                    .Where(v => v.Status == "Approved")
                    .Sum(v => v.EndDate.DayNumber - v.StartDate.DayNumber + 1),
                MostActiveUser = vacationType.Vacations
                    .GroupBy(v => new { v.UserId, v.User.FirstName, v.User.LastName })
                    .OrderByDescending(g => g.Count())
                    .FirstOrDefault()
            };

            ViewBag.UsageStats = usageStats;
            return View(vacationType);
        }

        private bool VacationTypeExists(Guid id)
        {
            return _context.VacationTypes.Any(e => e.Id == id);
        }
    }
}