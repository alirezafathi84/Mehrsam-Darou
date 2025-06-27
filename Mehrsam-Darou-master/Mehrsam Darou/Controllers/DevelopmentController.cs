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
            try
            {
                var setting = await ReadSettingAsync(_context);
                int pageSize = Convert.ToInt32(setting.NumberPerPage ?? 10);
                int pageNumber = page ?? 1;

                IQueryable<DevelopmentProject> query = _context.DevelopmentProjects;

                if (!string.IsNullOrWhiteSpace(searchKey))
                {
                    query = query.Where(d =>
                        (d.ProjectName != null && d.ProjectName.Contains(searchKey)) ||
                        (d.ProjectCode != null && d.ProjectCode.Contains(searchKey)) ||
                        (d.ProjectManager != null && d.ProjectManager.Contains(searchKey)) ||
                        (d.TeamLead != null && d.TeamLead.Contains(searchKey)) ||
                        (d.ProjectType != null && d.ProjectType.Contains(searchKey)))
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
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "خطا در بارگذاری لیست پروژه‌ها: " + ex.Message;
                return View(new PaginatedList<DevelopmentProject>(new List<DevelopmentProject>(), 0, 1, 10));
            }
        }

        // GET: Development/AddDevelopment
        public async Task<IActionResult> AddDevelopment()
        {
            try
            {
                ViewBag.Medicines = await _context.Medicines
                    .Where(m => m.IsActive == true)
                    .Select(m => new { m.MedicineId, m.BrandName })
                    .ToListAsync();

                return View(new DevelopmentProject
                {
                    ProjectId = Guid.NewGuid(),
                    IsActive = true,
                    CreatedDate = DateTime.Now,
                    PriorityLevel = 3,
                    Currency = "IRR",
                    ProjectStatus = "طرح اولیه",
                    ProgressPercentage = 0
                });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "خطا در بارگذاری فرم: " + ex.Message;
                return RedirectToAction(nameof(DevelopmentList));
            }
        }

        // POST: Development/AddDevelopment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddDevelopment(DevelopmentProject project)
        {
            try
            {
                // Remove validation for fields that might be causing issues
                ModelState.Remove("CreatedBy");
                ModelState.Remove("LastModifiedBy");
                ModelState.Remove("TargetMedicine");

                // Validate dates before ModelState.IsValid check
                if (project.PlannedStartDate.HasValue && project.PlannedEndDate.HasValue)
                {
                    if (project.PlannedEndDate < project.PlannedStartDate)
                    {
                        ModelState.AddModelError("PlannedEndDate", "تاریخ پایان باید بعد از تاریخ شروع باشد");
                    }
                }

                if (project.ActualStartDate.HasValue && project.ActualEndDate.HasValue)
                {
                    if (project.ActualEndDate < project.ActualStartDate)
                    {
                        ModelState.AddModelError("ActualEndDate", "تاریخ پایان واقعی باید بعد از تاریخ شروع واقعی باشد");
                    }
                }

                if (ModelState.IsValid)
                {
                    // Check for duplicate project code
                    if (await _context.DevelopmentProjects.AnyAsync(d => d.ProjectCode == project.ProjectCode))
                    {
                        ModelState.AddModelError("ProjectCode", "پروژه‌ای با این کد قبلاً ثبت شده است");
                        ViewBag.Medicines = await _context.Medicines
                            .Where(m => m.IsActive == true)
                            .Select(m => new { m.MedicineId, m.BrandName })
                            .ToListAsync();
                        return View(project);
                    }

                    // Ensure required fields are not null or empty
                    if (string.IsNullOrWhiteSpace(project.ProjectCode))
                    {
                        ModelState.AddModelError("ProjectCode", "کد پروژه الزامی است");
                        ViewBag.Medicines = await _context.Medicines
                            .Where(m => m.IsActive == true)
                            .Select(m => new { m.MedicineId, m.BrandName })
                            .ToListAsync();
                        return View(project);
                    }

                    if (string.IsNullOrWhiteSpace(project.ProjectName))
                    {
                        ModelState.AddModelError("ProjectName", "نام پروژه الزامی است");
                        ViewBag.Medicines = await _context.Medicines
                            .Where(m => m.IsActive == true)
                            .Select(m => new { m.MedicineId, m.BrandName })
                            .ToListAsync();
                        return View(project);
                    }

                    // Set default values
                    project.ProjectId = Guid.NewGuid();
                    project.CreatedDate = DateTime.Now;
                    project.IsActive = project.IsActive ?? true;
                    project.PriorityLevel = project.PriorityLevel ?? 3;
                    project.ProgressPercentage = project.ProgressPercentage ?? 0;
                    project.Currency = string.IsNullOrWhiteSpace(project.Currency) ? "IRR" : project.Currency;
                    project.ProjectStatus = string.IsNullOrWhiteSpace(project.ProjectStatus) ? "طرح اولیه" : project.ProjectStatus;

                    // Validate date constraints again to prevent database constraint violations
                    if (project.PlannedStartDate.HasValue && project.PlannedEndDate.HasValue &&
                        project.PlannedEndDate < project.PlannedStartDate)
                    {
                        // Clear invalid end date
                        project.PlannedEndDate = null;
                    }

                    if (project.ActualStartDate.HasValue && project.ActualEndDate.HasValue &&
                        project.ActualEndDate < project.ActualStartDate)
                    {
                        // Clear invalid end date
                        project.ActualEndDate = null;
                    }

                    // Calculate budget remaining
                    if (project.BudgetAllocated.HasValue && project.BudgetSpent.HasValue)
                    {
                        project.BudgetRemaining = project.BudgetAllocated - project.BudgetSpent;
                    }
                    else if (project.BudgetAllocated.HasValue)
                    {
                        project.BudgetRemaining = project.BudgetAllocated;
                    }

                    // Handle nullable foreign key
                    if (project.TargetMedicineId == Guid.Empty)
                    {
                        project.TargetMedicineId = null;
                    }

                    _context.Add(project);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "پروژه توسعه جدید با موفقیت ایجاد شد";
                    return RedirectToAction(nameof(DevelopmentList));
                }
            }
            catch (Exception ex)
            {
                // Log the full exception details for debugging
                var innerMessage = ex.InnerException?.Message ?? ex.Message;
                TempData["ErrorMessage"] = "خطا در ایجاد پروژه: " + innerMessage;

                // Add detailed error information for debugging
                System.Diagnostics.Debug.WriteLine($"Error creating project: {ex}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner exception: {ex.InnerException}");
                }
            }

            // Reload medicines for the view
            try
            {
                ViewBag.Medicines = await _context.Medicines
                    .Where(m => m.IsActive == true)
                    .Select(m => new { m.MedicineId, m.BrandName })
                    .ToListAsync();
            }
            catch
            {
                ViewBag.Medicines = new List<object>();
            }

            return View(project);
        }

        // GET: Development/EditDevelopment/5
        public async Task<IActionResult> EditDevelopment(Guid id)
        {
            try
            {
                var project = await _context.DevelopmentProjects
                    .FirstOrDefaultAsync(d => d.ProjectId == id);

                if (project == null)
                {
                    TempData["ErrorMessage"] = "پروژه مورد نظر یافت نشد";
                    return RedirectToAction(nameof(DevelopmentList));
                }

                ViewBag.Medicines = await _context.Medicines
                    .Where(m => m.IsActive == true)
                    .Select(m => new { m.MedicineId, m.BrandName })
                    .ToListAsync();

                return View(project);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "خطا در بارگذاری پروژه: " + ex.Message;
                return RedirectToAction(nameof(DevelopmentList));
            }
        }

        // POST: Development/EditDevelopment/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditDevelopment(Guid id, DevelopmentProject project)
        {
            if (id != project.ProjectId)
            {
                TempData["ErrorMessage"] = "شناسه پروژه نامعتبر است";
                return RedirectToAction(nameof(DevelopmentList));
            }

            try
            {
                // Remove validation for fields that might be causing issues
                ModelState.Remove("CreatedBy");
                ModelState.Remove("LastModifiedBy");
                ModelState.Remove("TargetMedicine");

                // Validate dates before ModelState.IsValid check
                if (project.PlannedStartDate.HasValue && project.PlannedEndDate.HasValue)
                {
                    if (project.PlannedEndDate < project.PlannedStartDate)
                    {
                        ModelState.AddModelError("PlannedEndDate", "تاریخ پایان باید بعد از تاریخ شروع باشد");
                    }
                }

                if (project.ActualStartDate.HasValue && project.ActualEndDate.HasValue)
                {
                    if (project.ActualEndDate < project.ActualStartDate)
                    {
                        ModelState.AddModelError("ActualEndDate", "تاریخ پایان واقعی باید بعد از تاریخ شروع واقعی باشد");
                    }
                }

                if (ModelState.IsValid)
                {
                    // Check for duplicate project code (excluding current project)
                    if (await _context.DevelopmentProjects.AnyAsync(d =>
                        d.ProjectId != id &&
                        d.ProjectCode == project.ProjectCode))
                    {
                        ModelState.AddModelError("ProjectCode", "پروژه‌ای با این کد قبلاً ثبت شده است");
                        ViewBag.Medicines = await _context.Medicines
                            .Where(m => m.IsActive == true)
                            .Select(m => new { m.MedicineId, m.BrandName })
                            .ToListAsync();
                        return View(project);
                    }

                    var existingProject = await _context.DevelopmentProjects.FindAsync(id);
                    if (existingProject == null)
                    {
                        TempData["ErrorMessage"] = "پروژه مورد نظر یافت نشد";
                        return RedirectToAction(nameof(DevelopmentList));
                    }

                    // Keep the original creation date and creator
                    project.CreatedDate = existingProject.CreatedDate;
                    project.CreatedBy = existingProject.CreatedBy;
                    project.LastModifiedDate = DateTime.Now;

                    // Set default values for required fields
                    project.IsActive = project.IsActive ?? true;
                    project.PriorityLevel = project.PriorityLevel ?? 3;
                    project.ProgressPercentage = project.ProgressPercentage ?? 0;
                    project.Currency = string.IsNullOrWhiteSpace(project.Currency) ? "IRR" : project.Currency;

                    // Validate date constraints again to prevent database constraint violations
                    if (project.PlannedStartDate.HasValue && project.PlannedEndDate.HasValue &&
                        project.PlannedEndDate < project.PlannedStartDate)
                    {
                        // Clear invalid end date
                        project.PlannedEndDate = null;
                    }

                    if (project.ActualStartDate.HasValue && project.ActualEndDate.HasValue &&
                        project.ActualEndDate < project.ActualStartDate)
                    {
                        // Clear invalid end date
                        project.ActualEndDate = null;
                    }

                    // Calculate budget remaining
                    if (project.BudgetAllocated.HasValue && project.BudgetSpent.HasValue)
                    {
                        project.BudgetRemaining = project.BudgetAllocated - project.BudgetSpent;
                    }
                    else if (project.BudgetAllocated.HasValue)
                    {
                        project.BudgetRemaining = project.BudgetAllocated;
                    }

                    // Handle nullable foreign key
                    if (project.TargetMedicineId == Guid.Empty)
                    {
                        project.TargetMedicineId = null;
                    }

                    _context.Entry(existingProject).CurrentValues.SetValues(project);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "اطلاعات پروژه با موفقیت به‌روزرسانی شد";
                    return RedirectToAction(nameof(DevelopmentList));
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!DevelopmentProjectExists(project.ProjectId))
                {
                    TempData["ErrorMessage"] = "پروژه مورد نظر یافت نشد";
                    return RedirectToAction(nameof(DevelopmentList));
                }
                else
                {
                    TempData["ErrorMessage"] = "خطا در به‌روزرسانی - پروژه توسط کاربر دیگری تغییر یافته است";
                }
            }
            catch (Exception ex)
            {
                var innerMessage = ex.InnerException?.Message ?? ex.Message;
                TempData["ErrorMessage"] = "خطا در به‌روزرسانی پروژه: " + innerMessage;
            }

            // Reload medicines for the view
            try
            {
                ViewBag.Medicines = await _context.Medicines
                    .Where(m => m.IsActive == true)
                    .Select(m => new { m.MedicineId, m.BrandName })
                    .ToListAsync();
            }
            catch
            {
                ViewBag.Medicines = new List<object>();
            }

            return View(project);
        }

        // POST: Development/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var project = await _context.DevelopmentProjects.FindAsync(id);
                if (project == null)
                {
                    TempData["ErrorMessage"] = "پروژه مورد نظر یافت نشد";
                    return RedirectToAction(nameof(DevelopmentList));
                }

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
                var innerMessage = ex.InnerException?.Message ?? ex.Message;
                TempData["ErrorMessage"] = "خطا در حذف پروژه: " + innerMessage;
            }

            return RedirectToAction(nameof(DevelopmentList));
        }

        // GET: Development/Details/5
        public async Task<IActionResult> Details(Guid id)
        {
            try
            {
                var project = await _context.DevelopmentProjects
                    .Include(d => d.TargetMedicine)
                    .FirstOrDefaultAsync(d => d.ProjectId == id);

                if (project == null)
                {
                    TempData["ErrorMessage"] = "پروژه مورد نظر یافت نشد";
                    return RedirectToAction(nameof(DevelopmentList));
                }

                return View(project);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "خطا در بارگذاری جزئیات پروژه: " + ex.Message;
                return RedirectToAction(nameof(DevelopmentList));
            }
        }

        private bool DevelopmentProjectExists(Guid id)
        {
            try
            {
                return _context.DevelopmentProjects.Any(e => e.ProjectId == id);
            }
            catch
            {
                return false;
            }
        }
    }
}