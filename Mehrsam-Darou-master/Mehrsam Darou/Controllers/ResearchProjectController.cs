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
            try
            {
                // Remove navigation property validation errors
                ModelState.Remove("CreatedByNavigation");
                ModelState.Remove("LastModifiedByNavigation");

                // Validate required fields manually
                if (string.IsNullOrWhiteSpace(researchProject.ProjectCode))
                {
                    TempData["ErrorMessage"] = "کد پروژه الزامی است";
                    return View(researchProject);
                }

                if (string.IsNullOrWhiteSpace(researchProject.ProjectTitle))
                {
                    TempData["ErrorMessage"] = "عنوان پروژه الزامی است";
                    return View(researchProject);
                }

                if (ModelState.IsValid)
                {
                    // Check for duplicate project code
                    if (await _context.ResearchProjects.AnyAsync(rp => rp.ProjectCode == researchProject.ProjectCode))
                    {
                        TempData["ErrorMessage"] = "پروژه با این کد قبلاً ثبت شده است";
                        return View(researchProject);
                    }

                    // Set required fields
                    researchProject.ProjectId = Guid.NewGuid();
                    researchProject.CreatedDate = DateTime.Now;

                    // Set default values if not provided
                    if (string.IsNullOrEmpty(researchProject.ProjectStatus))
                        researchProject.ProjectStatus = "طرح‌ریزی";

                    if (researchProject.PriorityLevel == 0)
                        researchProject.PriorityLevel = 3;

                    // Validate date constraints
                    if (researchProject.StartDate.HasValue && researchProject.PlannedEndDate.HasValue)
                    {
                        if (researchProject.PlannedEndDate < researchProject.StartDate)
                        {
                            TempData["ErrorMessage"] = "تاریخ پایان برنامه‌ریزی شده نمی‌تواند قبل از تاریخ شروع باشد";
                            return View(researchProject);
                        }
                    }

                    if (researchProject.StartDate.HasValue && researchProject.ActualEndDate.HasValue)
                    {
                        if (researchProject.ActualEndDate < researchProject.StartDate)
                        {
                            TempData["ErrorMessage"] = "تاریخ پایان واقعی نمی‌تواند قبل از تاریخ شروع باشد";
                            return View(researchProject);
                        }
                    }

                    // Clear problematic dates if they violate constraints
                    if (researchProject.StartDate.HasValue && researchProject.PlannedEndDate.HasValue)
                    {
                        if (researchProject.PlannedEndDate < researchProject.StartDate)
                            researchProject.PlannedEndDate = null;
                    }

                    if (researchProject.StartDate.HasValue && researchProject.ActualEndDate.HasValue)
                    {
                        if (researchProject.ActualEndDate < researchProject.StartDate)
                            researchProject.ActualEndDate = null;
                    }

                    // Clear navigation properties to avoid tracking issues
                    researchProject.CreatedByNavigation = null;
                    researchProject.LastModifiedByNavigation = null;

                    _context.Add(researchProject);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "پروژه تحقیقاتی جدید با موفقیت ایجاد شد";
                    return RedirectToAction(nameof(ResearchProjectList));
                }
                else
                {
                    // Show validation errors
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .Where(e => !string.IsNullOrEmpty(e))
                        .ToList();

                    if (errors.Any())
                    {
                        TempData["ErrorMessage"] = "خطاهای اعتبارسنجی: " + string.Join(", ", errors);
                    }
                }
            }
            catch (DbUpdateException dbEx)
            {
                var innerException = dbEx.InnerException?.Message ?? "No inner exception";
                TempData["ErrorMessage"] = $"خطا در پایگاه داده: {dbEx.Message} | جزئیات: {innerException}";
            }
            catch (Exception ex)
            {
                var innerException = ex.InnerException?.Message ?? "No inner exception";
                TempData["ErrorMessage"] = $"خطا در ایجاد پروژه: {ex.Message} | جزئیات: {innerException}";
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

            try
            {
                // Remove navigation property validation errors
                ModelState.Remove("CreatedByNavigation");
                ModelState.Remove("LastModifiedByNavigation");

                if (ModelState.IsValid)
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

                    // Validate date constraints for edit
                    if (researchProject.StartDate.HasValue && researchProject.PlannedEndDate.HasValue)
                    {
                        if (researchProject.PlannedEndDate < researchProject.StartDate)
                        {
                            TempData["ErrorMessage"] = "تاریخ پایان برنامه‌ریزی شده نمی‌تواند قبل از تاریخ شروع باشد";
                            return View(researchProject);
                        }
                    }

                    if (researchProject.StartDate.HasValue && researchProject.ActualEndDate.HasValue)
                    {
                        if (researchProject.ActualEndDate < researchProject.StartDate)
                        {
                            TempData["ErrorMessage"] = "تاریخ پایان واقعی نمی‌تواند قبل از تاریخ شروع باشد";
                            return View(researchProject);
                        }
                    }

                    // Clear navigation properties
                    researchProject.CreatedByNavigation = null;
                    researchProject.LastModifiedByNavigation = null;

                    _context.Entry(existingProject).CurrentValues.SetValues(researchProject);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "اطلاعات پروژه با موفقیت به‌روزرسانی شد";
                    return RedirectToAction(nameof(ResearchProjectList));
                }
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
            catch (Exception ex)
            {
                var innerException = ex.InnerException?.Message ?? "No inner exception";
                TempData["ErrorMessage"] = $"خطا در به‌روزرسانی پروژه: {ex.Message} | جزئیات: {innerException}";
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
                var innerException = ex.InnerException?.Message ?? "No inner exception";
                TempData["ErrorMessage"] = $"خطا در حذف پروژه: {ex.Message} | جزئیات: {innerException}";
            }

            return RedirectToAction(nameof(ResearchProjectList));
        }

        private bool ResearchProjectExists(Guid id)
        {
            return _context.ResearchProjects.Any(e => e.ProjectId == id);
        }
    }
}