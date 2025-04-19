using Mehrsam_Darou.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Mehrsam_Darou.Helper.Helper;

namespace Mehrsam_Darou.Controllers
{
    public class OrganizationController : BaseController
    {
        private readonly DarouAppContext _context;
        public OrganizationController(DarouAppContext context) : base(context)
        {
            _context = context;
        }


        public async Task<IActionResult> OrganizationList(int? page, string SearchKey)
        {



            // Set common view data and get the page size
            var setting = await ReadSettingAsync(_context);
            int pageSize = Convert.ToInt32(setting.NumberPerPage ?? 10); // Default to 10 if setting.NumberPerPage is null
            int pageNumber = page ?? 1;

            // Base query for fetching users
            IQueryable<Organization> query = _context.Organizations;

            // Apply search filter if SearchKey is provided
            if (!string.IsNullOrWhiteSpace(SearchKey))
            {
                query = query.Where(u => u.Name.Contains(SearchKey)).OrderBy(e => e.Name);
            }

            // Get total count after filtering
            int total = await query.CountAsync();

            // Fetch paginated results
            var Items = await query
                .OrderBy(u => u.Priority)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Create the paginated list
            var paginatedUsers = new PaginatedList<Organization>(Items, total, pageNumber, pageSize);

            ViewBag.Organization = await _context.Organizations.ToListAsync();

            // Pass paginated list to the view
            return View(paginatedUsers);
        }

        [HttpGet]
        public async Task<IActionResult> EditOrganization(Guid id)
        {
            var organization = await _context.Organizations.FindAsync(id);
            if (organization == null)
            {
                return NotFound();
            }

            return View(organization);
        }

        [HttpPost]
        public async Task<IActionResult> EditOrganization(Organization organization)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "اطلاعات وارد شده معتبر نیست.";
                return View("EditOrganization", organization);
            }

            var existingOrg = await _context.Organizations.FindAsync(organization.Id);
            if (existingOrg == null)
            {
                TempData["ErrorMessage"] = "سازمان مورد نظر یافت نشد.";
                return View("EditOrganization");
            }

            // Update properties from the submitted form
            existingOrg.Name = organization.Name;
            existingOrg.Priority = organization.Priority;

            try
            {
                _context.Organizations.Update(existingOrg);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "اطلاعات سازمان با موفقیت به‌روزرسانی شد.";
                return RedirectToAction("OrganizationList");
            }
            catch (Exception ex)
            {
                // Log the error (ex) here if you have logging configured
                TempData["ErrorMessage"] = "خطایی در به‌روزرسانی اطلاعات سازمان رخ داد.";
                return View("EditOrganization", organization);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteOrganization(Guid id)
        {
            var organization = await _context.Organizations.FindAsync(id);
            if (organization == null)
            {
                TempData["ErrorMessage"] = "سازمان مورد نظر یافت نشد.";
                return RedirectToAction("OrganizationList");
            }

            _context.Organizations.Remove(organization);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "سازمان با موفقیت حذف شد.";
            return RedirectToAction("OrganizationList");
        }


        [HttpGet]
        public IActionResult AddOrganization()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddOrganization(Organization organization)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    organization.Id = Guid.NewGuid();
                    _context.Organizations.Add(organization);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "سازمان با موفقیت ایجاد شد";
                    return RedirectToAction(nameof(OrganizationList));
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "خطا در ایجاد سازمان: " + ex.Message;
                }
            }
            else
            {
                TempData["ErrorMessage"] = "لطفاً اطلاعات را صحیح وارد نمایید";
            }

            return View(organization);
        }


    }
}
