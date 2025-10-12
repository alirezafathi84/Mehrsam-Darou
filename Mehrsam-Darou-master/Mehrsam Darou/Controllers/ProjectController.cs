using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mehrsam_Darou.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using static Mehrsam_Darou.Helper.Helper;

namespace Mehrsam_Darou.Controllers
{
    public class ProjectController : BaseController
    {
        private readonly DarouAppContext _context;

        public ProjectController(DarouAppContext context) : base(context)
        {
            _context = context;
        }

        // GET: Project/ProjectList
        public async Task<IActionResult> ProjectList(int? page, string searchKey)
        {
            var setting = await ReadSettingAsync(_context);
            int pageSize = Convert.ToInt32(setting.NumberPerPage ?? 10);
            int pageNumber = page ?? 1;

            IQueryable<Project> query = _context.Projects;

            if (!string.IsNullOrWhiteSpace(searchKey))
            {
                query = query.Where(p => p.ProjectName.Contains(searchKey) ||
                                     p.ProjectCode.Contains(searchKey) ||
                                     p.Description.Contains(searchKey) ||
                                     p.Status.Contains(searchKey))
                            .OrderBy(p => p.ProjectName);
            }

            int total = await query.CountAsync();
            var items = await query
                .OrderBy(p => p.ProjectName)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var paginatedList = new PaginatedList<Project>(items, total, pageNumber, pageSize);

            return View(paginatedList);
        }

        // GET: Project/AddProject
        public IActionResult AddProject()
        {
            return View(new Project
            {
                IsActive = true,
                CreatedDate = DateTime.Now,
                Status = "فعال"
            });
        }

        // POST: Project/AddProject
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddProject(Project project)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    if (await _context.Projects.AnyAsync(p => p.ProjectCode == project.ProjectCode))
                    {
                        TempData["ErrorMessage"] = "پروژه با این کد قبلاً ثبت شده است";
                        return View(project);
                    }

                    // Validate dates
                    if (project.StartDate.HasValue && project.EndDate.HasValue)
                    {
                        if (project.EndDate < project.StartDate)
                        {
                            TempData["ErrorMessage"] = "تاریخ پایان نمی‌تواند قبل از تاریخ شروع باشد";
                            return View(project);
                        }
                    }

                    project.ProjectId = Guid.NewGuid();
                    project.CreatedDate = DateTime.Now;

                    _context.Add(project);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "پروژه جدید با موفقیت ایجاد شد";
                    return RedirectToAction(nameof(ProjectList));
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "خطا در ایجاد پروژه: " + ex.Message;
                }
            }

            return View(project);
        }

        // GET: Project/EditProject/5
        public async Task<IActionResult> EditProject(Guid id)
        {
            var project = await _context.Projects.FindAsync(id);
            if (project == null)
            {
                return NotFound();
            }

            return View(project);
        }

        // POST: Project/EditProject/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProject(Guid id, Project project)
        {
            if (id != project.ProjectId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (await _context.Projects.AnyAsync(p =>
                        p.ProjectId != id &&
                        p.ProjectCode == project.ProjectCode))
                    {
                        TempData["ErrorMessage"] = "پروژه با این کد قبلاً ثبت شده است";
                        return View(project);
                    }

                    // Validate dates
                    if (project.StartDate.HasValue && project.EndDate.HasValue)
                    {
                        if (project.EndDate < project.StartDate)
                        {
                            TempData["ErrorMessage"] = "تاریخ پایان نمی‌تواند قبل از تاریخ شروع باشد";
                            return View(project);
                        }
                    }

                    var existingProject = await _context.Projects.FindAsync(id);
                    if (existingProject == null)
                    {
                        return NotFound();
                    }

                    // Keep the original creation date
                    project.CreatedDate = existingProject.CreatedDate;

                    _context.Entry(existingProject).CurrentValues.SetValues(project);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "اطلاعات پروژه با موفقیت به‌روزرسانی شد";
                    return RedirectToAction(nameof(ProjectList));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProjectExists(project.ProjectId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            return View(project);
        }

        // POST: Project/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var project = await _context.Projects.FindAsync(id);
            if (project == null)
            {
                TempData["ErrorMessage"] = "پروژه مورد نظر یافت نشد";
                return RedirectToAction(nameof(ProjectList));
            }

            // Check if project has any material requests
            bool hasMaterialRequests = await _context.MaterialRequests.AnyAsync(m => m.ProjectId == id);

            if (hasMaterialRequests)
            {
                TempData["ErrorMessage"] = "این پروژه دارای درخواست مواد است و قابل حذف نیست";
                return RedirectToAction(nameof(ProjectList));
            }

            try
            {
                _context.Projects.Remove(project);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "پروژه با موفقیت حذف شد";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "خطا در حذف پروژه: " + ex.Message;
            }

            return RedirectToAction(nameof(ProjectList));
        }

        private bool ProjectExists(Guid id)
        {
            return _context.Projects.Any(e => e.ProjectId == id);
        }
    }
}