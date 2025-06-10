using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mehrsam_Darou.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using static Mehrsam_Darou.Helper.Helper;

namespace Mehrsam_Darou.Controllers
{
    public class ResearchProjectController : BaseController
    {
        private readonly DarouAppContext _context;

        public ResearchProjectController(DarouAppContext context) : base(context)
        {
            _context = context;
        }

        // GET: ResearchProject/ResearchProjectList
        public async Task<IActionResult> ResearchProjectList(int? page, string searchKey, string status)
        {
            var setting = await ReadSettingAsync(_context);
            int pageSize = Convert.ToInt32(setting.NumberPerPage ?? 10);
            int pageNumber = page ?? 1;

            IQueryable<ResearchProject> query = _context.ResearchProjects;

            // Filter by status if provided
            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(rp => rp.ProjectStatus == status);
            }

            // Search functionality
            if (!string.IsNullOrWhiteSpace(searchKey))
            {
                query = query.Where(rp => rp.ProjectCode.Contains(searchKey) ||
                                     rp.ProjectTitle.Contains(searchKey) ||
                                     rp.ProjectType.Contains(searchKey) ||
                                     rp.PrincipalInvestigator.Contains(searchKey) ||
                                     rp.ProjectManager.Contains(searchKey))
                            .OrderByDescending(rp => rp.CreatedDate);
            }
            else
            {
                query = query.OrderByDescending(rp => rp.CreatedDate);
            }

            int total = await query.CountAsync();
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var paginatedList = new PaginatedList<ResearchProject>(items, total, pageNumber, pageSize);

            // Pass filter values to view
            ViewBag.CurrentStatus = status;
            ViewBag.CurrentSearch = searchKey;

            return View(paginatedList);
        }

        // GET: ResearchProject/AddResearchProject
        public IActionResult AddResearchProject()
        {
            return View(new ResearchProject
            {
                IsActive = true,
                CreatedDate = DateTime.Now,
                ProjectStatus = "طرح‌ریزی",
                PriorityLevel = 3,
                ProjectType = "تحقیق و توسعه"
            });
        }

        // POST: ResearchProject/AddResearchProject
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddResearchProject(ResearchProject researchProject)
        {
            // Remove navigation property validation errors
            ModelState.Remove("CreatedByNavigation");
            ModelState.Remove("LastModifiedByNavigation");

            if (ModelState.IsValid)
            {
                try
                {
                    if (await _context.ResearchProjects.AnyAsync(rp => rp.ProjectCode == researchProject.ProjectCode))
                    {
                        TempData["ErrorMessage"] = "پروژه با این کد قبلاً ثبت شده است";
                        return View(researchProject);
                    }

                    researchProject.ProjectId = Guid.NewGuid();
                    researchProject.CreatedDate = DateTime.Now;

                    _context.Add(researchProject);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "پروژه تحقیقاتی جدید با موفقیت ایجاد شد";
                    return RedirectToAction(nameof(ResearchProjectList));
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "خطا در ایجاد پروژه: " + ex.Message;
                }
            }

            return View(researchProject);
        }

        // GET: ResearchProject/EditResearchProject/5
        public async Task<IActionResult> EditResearchProject(Guid id)
        {
            var researchProject = await _context.ResearchProjects.FindAsync(id);
            if (researchProject == null)
            {
                return NotFound();
            }

            return View(researchProject);
        }

        // POST: ResearchProject/EditResearchProject/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditResearchProject(Guid id, ResearchProject researchProject)
        {
            if (id != researchProject.ProjectId)
            {
                return NotFound();
            }

            // Remove navigation property validation errors
            ModelState.Remove("CreatedByNavigation");
            ModelState.Remove("LastModifiedByNavigation");

            if (ModelState.IsValid)
            {
                try
                {
                    if (await _context.ResearchProjects.AnyAsync(rp =>
                        rp.ProjectId != id &&
                        rp.ProjectCode == researchProject.ProjectCode))
                    {
                        TempData["ErrorMessage"] = "پروژه با این کد قبلاً ثبت شده است";
                        return View(researchProject);
                    }

                    var existingProject = await _context.ResearchProjects.FindAsync(id);
                    if (existingProject == null)
                    {
                        return NotFound();
                    }

                    // Keep the original creation date
                    researchProject.CreatedDate = existingProject.CreatedDate;
                    researchProject.LastModifiedDate = DateTime.Now;

                    _context.Entry(existingProject).CurrentValues.SetValues(researchProject);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "اطلاعات پروژه با موفقیت به‌روزرسانی شد";
                    return RedirectToAction(nameof(ResearchProjectList));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ResearchProjectExists(researchProject.ProjectId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            return View(researchProject);
        }

        // POST: ResearchProject/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var researchProject = await _context.ResearchProjects.FindAsync(id);
            if (researchProject == null)
            {
                TempData["ErrorMessage"] = "پروژه مورد نظر یافت نشد";
                return RedirectToAction(nameof(ResearchProjectList));
            }

            try
            {
                _context.ResearchProjects.Remove(researchProject);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "پروژه با موفقیت حذف شد";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "خطا در حذف پروژه: " + ex.Message;
            }

            return RedirectToAction(nameof(ResearchProjectList));
        }

        private bool ResearchProjectExists(Guid id)
        {
            return _context.ResearchProjects.Any(e => e.ProjectId == id);
        }
    }
}