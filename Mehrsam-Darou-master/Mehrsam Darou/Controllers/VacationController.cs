using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mehrsam_Darou.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using static Mehrsam_Darou.Helper.Helper;

namespace Mehrsam_Darou.Controllers
{
    public class VacationController : BaseController
    {
        private readonly DarouAppContext _context;

        public VacationController(DarouAppContext context) : base(context)
        {
            _context = context;
        }

        // GET: Vacation/VacationList
        public async Task<IActionResult> VacationList(int? page, string searchKey, string status)
        {
            var setting = await ReadSettingAsync(_context);
            int pageSize = Convert.ToInt32(setting.NumberPerPage ?? 10);
            int pageNumber = page ?? 1;

            IQueryable<Vacation> query = _context.Vacations
                .Include(v => v.User)
                .Include(v => v.Type)
                .Include(v => v.ApprovedByNavigation);

            if (!string.IsNullOrWhiteSpace(searchKey))
            {
                query = query.Where(v => v.User.FirstName.Contains(searchKey) ||
                                     v.User.LastName.Contains(searchKey) ||
                                     v.User.Username.Contains(searchKey) ||
                                     v.Type.Name.Contains(searchKey) ||
                                     v.Notes.Contains(searchKey));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(v => v.Status == status);
            }

            query = query.OrderByDescending(v => v.DateCreated);

            int total = await query.CountAsync();
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var paginatedList = new PaginatedList<Vacation>(items, total, pageNumber, pageSize);

            ViewBag.StatusFilter = status;
            return View(paginatedList);
        }

        // GET: Vacation/AddVacation
        public async Task<IActionResult> AddVacation()
        {
            ViewBag.Users = await _context.Users
                .Where(u => u.TeamId != null)
                .OrderBy(u => u.FirstName)
                .ThenBy(u => u.LastName)
                .Select(u => new { u.Id, FullName = u.FirstName + " " + u.LastName })
                .ToListAsync();

            ViewBag.VacationTypes = await _context.VacationTypes
                .Where(vt => vt.Id != null)
                .OrderBy(vt => vt.Name)
                .Select(vt => new { vt.Id, vt.Name, vt.Description, vt.IsPaid, vt.MaxDaysPerYear })
                .ToListAsync();

            return View(new Vacation
            {
                StartDate = DateOnly.FromDateTime(DateTime.Now),
                EndDate = DateOnly.FromDateTime(DateTime.Now.AddDays(1)),
                Status = "Pending",
                DateCreated = DateTime.Now
            });
        }

        // POST: Vacation/AddVacation
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddVacation(Vacation vacation)
        {
            // Remove navigation properties and auto-set properties from ModelState validation
            ModelState.Remove("User");
            ModelState.Remove("Type");
            ModelState.Remove("ApprovedByNavigation");
            ModelState.Remove("Status"); // Status is set programmatically to "Pending"

            if (ModelState.IsValid)
            {
                try
                {
                    // Validate date range
                    if (vacation.EndDate <= vacation.StartDate)
                    {
                        TempData["ErrorMessage"] = "تاریخ پایان باید بعد از تاریخ شروع باشد";
                        await LoadViewBagData();
                        return View(vacation);
                    }

                    // Check for overlapping vacations
                    var overlappingVacation = await _context.Vacations
                        .Where(v => v.UserId == vacation.UserId &&
                                   v.Status != "Rejected" &&
                                   v.Id != vacation.Id &&
                                   ((v.StartDate <= vacation.StartDate && v.EndDate >= vacation.StartDate) ||
                                    (v.StartDate <= vacation.EndDate && v.EndDate >= vacation.EndDate) ||
                                    (v.StartDate >= vacation.StartDate && v.EndDate <= vacation.EndDate)))
                        .FirstOrDefaultAsync();

                    if (overlappingVacation != null)
                    {
                        TempData["ErrorMessage"] = "در این بازه زمانی درخواست مرخصی دیگری وجود دارد";
                        await LoadViewBagData();
                        return View(vacation);
                    }

                    vacation.Id = Guid.NewGuid();
                    vacation.DateCreated = DateTime.Now;
                    vacation.PersianStartDate = ConvertToPersianDate(vacation.StartDate);
                    vacation.PersianEndDate = ConvertToPersianDate(vacation.EndDate);
                    vacation.Status = "Pending"; // Always set to Pending for new requests

                    _context.Add(vacation);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "درخواست مرخصی با موفقیت ثبت شد";
                    return RedirectToAction(nameof(VacationList));
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "خطا در ثبت درخواست مرخصی: " + ex.Message;
                }
            }

            await LoadViewBagData();
            return View(vacation);
        }

        // GET: Vacation/EditVacation/5
        public async Task<IActionResult> EditVacation(Guid id)
        {
            var vacation = await _context.Vacations
                .Include(v => v.User)
                .Include(v => v.Type)
                .Include(v => v.ApprovedByNavigation)
                .FirstOrDefaultAsync(v => v.Id == id);

            if (vacation == null)
            {
                return NotFound();
            }

            await LoadViewBagData();
            return View(vacation);
        }

        // POST: Vacation/EditVacation/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditVacation(Guid id, Vacation vacation)
        {
            if (id != vacation.Id)
            {
                return NotFound();
            }

            // Remove navigation properties from ModelState validation
            ModelState.Remove("User");
            ModelState.Remove("Type");
            ModelState.Remove("ApprovedByNavigation");

            if (ModelState.IsValid)
            {
                try
                {
                    // Validate date range
                    if (vacation.EndDate <= vacation.StartDate)
                    {
                        TempData["ErrorMessage"] = "تاریخ پایان باید بعد از تاریخ شروع باشد";
                        await LoadViewBagData();
                        return View(vacation);
                    }

                    var existingVacation = await _context.Vacations.FindAsync(id);
                    if (existingVacation == null)
                    {
                        return NotFound();
                    }

                    // Keep the original creation date
                    vacation.DateCreated = existingVacation.DateCreated;
                    vacation.PersianStartDate = ConvertToPersianDate(vacation.StartDate);
                    vacation.PersianEndDate = ConvertToPersianDate(vacation.EndDate);

                    // Set approval date if status is being changed to Approved
                    if (vacation.Status == "Approved" && existingVacation.Status != "Approved")
                    {
                        vacation.ApprovalDate = DateTime.Now;
                        // You might want to set ApprovedBy to current user ID
                        // vacation.ApprovedBy = GetCurrentUserId();
                    }

                    _context.Entry(existingVacation).CurrentValues.SetValues(vacation);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "درخواست مرخصی با موفقیت به‌روزرسانی شد";
                    return RedirectToAction(nameof(VacationList));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!VacationExists(vacation.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            await LoadViewBagData();
            return View(vacation);
        }

        // POST: Vacation/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var vacation = await _context.Vacations
                .Include(v => v.User)
                .Include(v => v.Type)
                .FirstOrDefaultAsync(v => v.Id == id);

            if (vacation == null)
            {
                TempData["ErrorMessage"] = "درخواست مرخصی مورد نظر یافت نشد";
                return RedirectToAction(nameof(VacationList));
            }

            // Check if vacation can be deleted (only pending or rejected vacations)
            if (vacation.Status == "Approved" && vacation.StartDate <= DateOnly.FromDateTime(DateTime.Now))
            {
                TempData["ErrorMessage"] = "نمی‌توان مرخصی تایید شده و شروع شده را حذف کرد";
                return RedirectToAction(nameof(VacationList));
            }

            try
            {
                _context.Vacations.Remove(vacation);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "درخواست مرخصی با موفقیت حذف شد";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "خطا در حذف درخواست مرخصی: " + ex.Message;
            }

            return RedirectToAction(nameof(VacationList));
        }

        // POST: Vacation/Approve/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(Guid id)
        {
            var vacation = await _context.Vacations.FindAsync(id);
            if (vacation == null)
            {
                TempData["ErrorMessage"] = "درخواست مرخصی یافت نشد";
                return RedirectToAction(nameof(VacationList));
            }

            vacation.Status = "Approved";
            vacation.ApprovalDate = DateTime.Now;
            // vacation.ApprovedBy = GetCurrentUserId(); // Set current user as approver

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "درخواست مرخصی تایید شد";
            return RedirectToAction(nameof(VacationList));
        }

        // POST: Vacation/Reject/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(Guid id)
        {
            var vacation = await _context.Vacations.FindAsync(id);
            if (vacation == null)
            {
                TempData["ErrorMessage"] = "درخواست مرخصی یافت نشد";
                return RedirectToAction(nameof(VacationList));
            }

            vacation.Status = "Rejected";
            vacation.ApprovalDate = DateTime.Now;
            // vacation.ApprovedBy = GetCurrentUserId(); // Set current user as approver

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "درخواست مرخصی رد شد";
            return RedirectToAction(nameof(VacationList));
        }

        private bool VacationExists(Guid id)
        {
            return _context.Vacations.Any(e => e.Id == id);
        }

        private async Task LoadViewBagData()
        {
            ViewBag.Users = await _context.Users
                .Where(u => u.TeamId != null)
                .OrderBy(u => u.FirstName)
                .ThenBy(u => u.LastName)
                .Select(u => new { u.Id, FullName = u.FirstName + " " + u.LastName })
                .ToListAsync();

            ViewBag.VacationTypes = await _context.VacationTypes
                .Where(vt => vt.Id != null)
                .OrderBy(vt => vt.Name)
                .Select(vt => new { vt.Id, vt.Name, vt.Description, vt.IsPaid, vt.MaxDaysPerYear })
                .ToListAsync();

            ViewBag.Approvers = await _context.Users
                .Where(u => u.TeamId != null)
                .OrderBy(u => u.FirstName)
                .ThenBy(u => u.LastName)
                .Select(u => new { u.Id, FullName = u.FirstName + " " + u.LastName })
                .ToListAsync();
        }

        private string ConvertToPersianDate(DateOnly date)
        {
            var dateTime = date.ToDateTime(TimeOnly.MinValue);
            System.Globalization.PersianCalendar pc = new System.Globalization.PersianCalendar();
            return $"{pc.GetYear(dateTime)}/{pc.GetMonth(dateTime):00}/{pc.GetDayOfMonth(dateTime):00}";
        }
    }
}