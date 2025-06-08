using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mehrsam_Darou.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using static Mehrsam_Darou.Helper.Helper;

namespace Mehrsam_Darou.Controllers
{
    public class SalaryInfoController : BaseController
    {
        private readonly DarouAppContext _context;

        public SalaryInfoController(DarouAppContext context) : base(context)
        {
            _context = context;
        }

        // GET: SalaryInfo/SalaryInfoList
        public async Task<IActionResult> SalaryInfoList(int? page, string searchKey)
        {
            var setting = await ReadSettingAsync(_context);
            int pageSize = Convert.ToInt32(setting.NumberPerPage ?? 10);
            int pageNumber = page ?? 1;

            IQueryable<SalaryInfo> query = _context.SalaryInfos
                .Include(s => s.User);

            if (!string.IsNullOrWhiteSpace(searchKey))
            {
                query = query.Where(s => s.User.FirstName.Contains(searchKey) ||
                                     s.User.LastName.Contains(searchKey) ||
                                     s.User.Username.Contains(searchKey) ||
                                     s.Currency.Contains(searchKey))
                            .OrderBy(s => s.User.FirstName);
            }

            int total = await query.CountAsync();
            var items = await query
                .OrderBy(s => s.User.FirstName)
                .ThenBy(s => s.EffectiveDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var paginatedList = new PaginatedList<SalaryInfo>(items, total, pageNumber, pageSize);

            return View(paginatedList);
        }

        // GET: SalaryInfo/AddSalaryInfo
        public async Task<IActionResult> AddSalaryInfo()
        {
            ViewBag.Users = await _context.Users
                .Where(u => u.TeamId != null)
                .OrderBy(u => u.FirstName)
                .ThenBy(u => u.LastName)
                .Select(u => new { u.Id, FullName = u.FirstName + " " + u.LastName })
                .ToListAsync();

            return View(new SalaryInfo
            {
                EffectiveDate = DateOnly.FromDateTime(DateTime.Now),
                Currency = "IRR",
                DateCreated = DateTime.Now
            });
        }

        // POST: SalaryInfo/AddSalaryInfo
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddSalaryInfo(SalaryInfo salaryInfo)
        {
            // Remove User from ModelState validation since it's a navigation property
            ModelState.Remove("User");

            if (ModelState.IsValid)
            {
                try
                {
                    // Check if there's an active salary info for this user
                    var existingActiveSalary = await _context.SalaryInfos
                        .Where(s => s.UserId == salaryInfo.UserId && s.EndDate == null)
                        .FirstOrDefaultAsync();

                    if (existingActiveSalary != null)
                    {
                        // End the previous salary info
                        existingActiveSalary.EndDate = salaryInfo.EffectiveDate.AddDays(-1);
                        existingActiveSalary.PersianEndDate = ConvertToPersianDate(existingActiveSalary.EndDate.Value);
                        _context.Update(existingActiveSalary);
                    }

                    salaryInfo.Id = Guid.NewGuid();
                    salaryInfo.DateCreated = DateTime.Now;
                    salaryInfo.PersianEffectiveDate = ConvertToPersianDate(salaryInfo.EffectiveDate);

                    _context.Add(salaryInfo);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "اطلاعات حقوق و دستمزد با موفقیت ایجاد شد";
                    return RedirectToAction(nameof(SalaryInfoList));
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "خطا در ایجاد اطلاعات حقوق و دستمزد: " + ex.Message;
                }
            }

            ViewBag.Users = await _context.Users
                .Where(u => u.TeamId != null)
                .OrderBy(u => u.FirstName)
                .ThenBy(u => u.LastName)
                .Select(u => new { u.Id, FullName = u.FirstName + " " + u.LastName })
                .ToListAsync();

            return View(salaryInfo);
        }

        // GET: SalaryInfo/EditSalaryInfo/5
        public async Task<IActionResult> EditSalaryInfo(Guid id)
        {
            var salaryInfo = await _context.SalaryInfos
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (salaryInfo == null)
            {
                return NotFound();
            }

            ViewBag.Users = await _context.Users
                .Where(u => u.TeamId != null)
                .OrderBy(u => u.FirstName)
                .ThenBy(u => u.LastName)
                .Select(u => new { u.Id, FullName = u.FirstName + " " + u.LastName })
                .ToListAsync();

            return View(salaryInfo);
        }

        // POST: SalaryInfo/EditSalaryInfo/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSalaryInfo(Guid id, SalaryInfo salaryInfo)
        {
            if (id != salaryInfo.Id)
            {
                return NotFound();
            }

            // Remove User from ModelState validation since it's a navigation property
            ModelState.Remove("User");

            if (ModelState.IsValid)
            {
                try
                {
                    var existingSalaryInfo = await _context.SalaryInfos.FindAsync(id);
                    if (existingSalaryInfo == null)
                    {
                        return NotFound();
                    }

                    // Keep the original creation date
                    salaryInfo.DateCreated = existingSalaryInfo.DateCreated;
                    salaryInfo.PersianEffectiveDate = ConvertToPersianDate(salaryInfo.EffectiveDate);

                    if (salaryInfo.EndDate.HasValue)
                    {
                        salaryInfo.PersianEndDate = ConvertToPersianDate(salaryInfo.EndDate.Value);
                    }

                    _context.Entry(existingSalaryInfo).CurrentValues.SetValues(salaryInfo);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "اطلاعات حقوق و دستمزد با موفقیت به‌روزرسانی شد";
                    return RedirectToAction(nameof(SalaryInfoList));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SalaryInfoExists(salaryInfo.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            ViewBag.Users = await _context.Users
                .Where(u => u.TeamId != null)
                .OrderBy(u => u.FirstName)
                .ThenBy(u => u.LastName)
                .Select(u => new { u.Id, FullName = u.FirstName + " " + u.LastName })
                .ToListAsync();

            return View(salaryInfo);
        }

        // POST: SalaryInfo/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var salaryInfo = await _context.SalaryInfos
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (salaryInfo == null)
            {
                TempData["ErrorMessage"] = "اطلاعات حقوق و دستمزد مورد نظر یافت نشد";
                return RedirectToAction(nameof(SalaryInfoList));
            }

            try
            {
                _context.SalaryInfos.Remove(salaryInfo);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "اطلاعات حقوق و دستمزد با موفقیت حذف شد";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "خطا در حذف اطلاعات حقوق و دستمزد: " + ex.Message;
            }

            return RedirectToAction(nameof(SalaryInfoList));
        }

        private bool SalaryInfoExists(Guid id)
        {
            return _context.SalaryInfos.Any(e => e.Id == id);
        }

        private string ConvertToPersianDate(DateOnly date)
        {
            var dateTime = date.ToDateTime(TimeOnly.MinValue);
            System.Globalization.PersianCalendar pc = new System.Globalization.PersianCalendar();
            return $"{pc.GetYear(dateTime)}/{pc.GetMonth(dateTime):00}/{pc.GetDayOfMonth(dateTime):00}";
        }
    }
}