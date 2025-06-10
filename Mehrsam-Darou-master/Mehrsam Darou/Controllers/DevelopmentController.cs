using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mehrsam_Darou.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using static Mehrsam_Darou.Helper.Helper;

namespace Mehrsam_Darou.Controllers
{
    public class DevelopmentController : BaseController
    {
        private readonly DarouAppContext _context;

        public DevelopmentController(DarouAppContext context) : base(context)
        {
            _context = context;
        }

        // GET: Development/DevelopmentList
        public async Task<IActionResult> DevelopmentList(int? page, string searchKey)
        {
            var setting = await ReadSettingAsync(_context);
            int pageSize = Convert.ToInt32(setting.NumberPerPage ?? 10);
            int pageNumber = page ?? 1;

            IQueryable<DevelopmentProject> query = _context.DevelopmentProjects;

            if (!string.IsNullOrWhiteSpace(searchKey))
            {
                query = query.Where(d => d.ProjectName.Contains(searchKey) ||
                                     d.ProjectCode.Contains(searchKey) ||
                                     d.ProjectManager.Contains(searchKey) ||
                                     d.TeamLead.Contains(searchKey) ||
                                     d.ProjectType.Contains(searchKey))
                            .OrderBy(d => d.ProjectName);
            }

            int total = await query.CountAsync();
            var items = await query
                .OrderByDescending(d => d.CreatedDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var paginatedList = new PaginatedList<DevelopmentProject>(items, total, pageNumber, pageSize);

            return View(paginatedList);
        }

        // GET: Development/AddDevelopment
        public async Task<IActionResult> AddDevelopment()
        {
            ViewBag.Medicines = await _context.Medicines
                .Where(m => m.IsActive == true)
                .Select(m => new { m.MedicineId, m.BrandName })
                .ToListAsync();

            return View(new DevelopmentProject
            {
                IsActive = true,
                CreatedDate = DateTime.Now,
                PriorityLevel = 3,
                Currency = "IRR",
                ProjectStatus = "طرح اولیه",
                ProgressPercentage = 0
            });
        }

        // POST: Development/AddDevelopment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddDevelopment(DevelopmentProject project)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    if (await _context.DevelopmentProjects.AnyAsync(d => d.ProjectCode == project.ProjectCode))
                    {
                        TempData["ErrorMessage"] = "پروژه‌ای با این کد قبلاً ثبت شده است";
                        ViewBag.Medicines = await _context.Medicines
                            .Where(m => m.IsActive == true)
                            .Select(m => new { m.MedicineId, m.BrandName })
                            .ToListAsync();
                        return View(project);
                    }

                    project.ProjectId = Guid.NewGuid();
                    project.CreatedDate = DateTime.Now;

                    // Calculate budget remaining
                    if (project.BudgetAllocated.HasValue && project.BudgetSpent.HasValue)
                    {
                        project.BudgetRemaining = project.BudgetAllocated - project.BudgetSpent;
                    }

                    _context.Add(project);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "پروژه توسعه جدید با موفقیت ایجاد شد";
                    return RedirectToAction(nameof(DevelopmentList));
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "خطا در ایجاد پروژه: " + ex.Message;
                }
            }

            ViewBag.Medicines = await _context.Medicines
                .Where(m => m.IsActive == true)
                .Select(m => new { m.MedicineId, m.BrandName })
                .ToListAsync();

            return View(project);
        }

        // GET: Development/EditDevelopment/5
        public async Task<IActionResult> EditDevelopment(Guid id)
        {
            var project = await _context.DevelopmentProjects
                .FirstOrDefaultAsync(d => d.ProjectId == id);

            if (project == null)
            {
                return NotFound();
            }

            ViewBag.Medicines = await _context.Medicines
                .Where(m => m.IsActive == true)
                .Select(m => new { m.MedicineId, m.BrandName })
                .ToListAsync();

            return View(project);
        }

        // POST: Development/EditDevelopment/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditDevelopment(Guid id, DevelopmentProject project)
        {
            if (id != project.ProjectId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (await _context.DevelopmentProjects.AnyAsync(d =>
                        d.ProjectId != id &&
                        d.ProjectCode == project.ProjectCode))
                    {
                        TempData["ErrorMessage"] = "پروژه‌ای با این کد قبلاً ثبت شده است";
                        ViewBag.Medicines = await _context.Medicines
                            .Where(m => m.IsActive == true)
                            .Select(m => new { m.MedicineId, m.BrandName })
                            .ToListAsync();
                        return View(project);
                    }

                    var existingProject = await _context.DevelopmentProjects.FindAsync(id);
                    if (existingProject == null)
                    {
                        return NotFound();
                    }

                    // Keep the original creation date and creator
                    project.CreatedDate = existingProject.CreatedDate;
                    project.CreatedBy = existingProject.CreatedBy;
                    project.LastModifiedDate = DateTime.Now;

                    // Calculate budget remaining
                    if (project.BudgetAllocated.HasValue && project.BudgetSpent.HasValue)
                    {
                        project.BudgetRemaining = project.BudgetAllocated - project.BudgetSpent;
                    }

                    _context.Entry(existingProject).CurrentValues.SetValues(project);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "اطلاعات پروژه با موفقیت به‌روزرسانی شد";
                    return RedirectToAction(nameof(DevelopmentList));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DevelopmentProjectExists(project.ProjectId))
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
                    TempData["ErrorMessage"] = "خطا در به‌روزرسانی پروژه: " + ex.Message;
                }
            }

            ViewBag.Medicines = await _context.Medicines
                .Where(m => m.IsActive == true)
                .Select(m => new { m.MedicineId, m.BrandName })
                .ToListAsync();

            return View(project);
        }

        // POST: Development/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var project = await _context.DevelopmentProjects.FindAsync(id);
            if (project == null)
            {
                TempData["ErrorMessage"] = "پروژه مورد نظر یافت نشد";
                return RedirectToAction(nameof(DevelopmentList));
            }

            try
            {
                // Check if project is completed or has important data before deletion
                if (project.ProjectStatus == "تکمیل شده" && project.ProgressPercentage >= 100)
                {
                    TempData["ErrorMessage"] = "پروژه تکمیل شده قابل حذف نیست. می‌توانید آن را غیرفعال کنید";
                    return RedirectToAction(nameof(DevelopmentList));
                }

                _context.DevelopmentProjects.Remove(project);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "پروژه با موفقیت حذف شد";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "خطا در حذف پروژه: " + ex.Message;
            }

            return RedirectToAction(nameof(DevelopmentList));
        }

        // GET: Development/Details/5
        public async Task<IActionResult> Details(Guid id)
        {
            var project = await _context.DevelopmentProjects
                .Include(d => d.TargetMedicine)
                .FirstOrDefaultAsync(d => d.ProjectId == id);

            if (project == null)
            {
                return NotFound();
            }

            return View(project);
        }

        private bool DevelopmentProjectExists(Guid id)
        {
            return _context.DevelopmentProjects.Any(e => e.ProjectId == id);
        }
    }
}