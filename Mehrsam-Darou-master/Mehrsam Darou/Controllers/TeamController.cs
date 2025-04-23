using Mehrsam_Darou.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Mehrsam_Darou.Helper.Helper;

namespace Mehrsam_Darou.Controllers
{
    public class TeamController : BaseController
    {
        private readonly ILogger<UserController> _logger;
        private readonly DarouAppContext _context;

        // Combine both constructors into one
        public TeamController(DarouAppContext context) : base(context)
        {
            _context = context;
        }

        public async Task<IActionResult> TeamList(int? page, string SearchKey)
        {




            // Set common view data and get the page size
            var setting = await ReadSettingAsync(_context);
            int pageSize = Convert.ToInt32(setting.NumberPerPage ?? 10); // Default to 10 if setting.NumberPerPage is null
            int pageNumber = page ?? 1;

            // Base query for fetching users
            IQueryable<Team> query = _context.Teams.Include(u => u.Users);

            // Apply search filter if SearchKey is provided
            if (!string.IsNullOrWhiteSpace(SearchKey))
            {
                query = query.Where(u => u.Name.Contains(SearchKey)).OrderBy(e => e.Name);
            }

            // Get total count after filtering
            int total = await query.CountAsync();

            // Fetch paginated results
            var teams = await query
                .OrderBy(u => u.Name)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Create the paginated list
            var paginatedUsers = new PaginatedList<Team>(teams, total, pageNumber, pageSize);

            ViewBag.Teams = await _context.Teams.ToListAsync();

            // Pass paginated list to the view
            return View(paginatedUsers);
        }



        public async Task<IActionResult> AddNewTeam()
        {
   

            return View("AddTeam");
        }

        [HttpPost]
        public async Task<IActionResult> AddTeam(Team team)
        {


            // Add the user to the database
            _context.Teams.Add(team);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "تیم با موفقیت اضافه شد.";
            return RedirectToAction("TeamList");
        }





        [HttpGet]
        public async Task<IActionResult> EditTeam(Guid id)
        {            // Validate session and get the user


            var team = await _context.Teams.FindAsync(id);
            if (team == null)
            {
                return NotFound();
            }

            return View(team);
        }

        [HttpPost]
        public async Task<IActionResult> EditTeam(Team team)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "اطلاعات وارد شده معتبر نیست.";
                return View("EditTeam", team);
            }

            var existingTeam = await _context.Teams.FindAsync(team.Id);
            if (existingTeam == null)
            {
                TempData["ErrorMessage"] = "تیم مورد نظر یافت نشد.";
                return View("EditTeam");
            }

            // Update all properties from the submitted form
            existingTeam.Name = team.Name;
            existingTeam.DefaultPageForTeam = team.DefaultPageForTeam;

            // Update boolean properties (checkbox values)
            existingTeam.IsActive = team.IsActive;
            existingTeam.ManagmentDashboard = team.ManagmentDashboard;
            existingTeam.Setting = team.Setting;
            existingTeam.SystemUsers = team.SystemUsers;
            existingTeam.Financial = team.Financial;
            existingTeam.Inventory = team.Inventory;
            existingTeam.Product = team.Product;
            existingTeam.SellCommercial = team.SellCommercial;
            existingTeam.BuyCommercial = team.BuyCommercial;
            existingTeam.RandD = team.RandD;
            existingTeam.Qc = team.Qc;
            existingTeam.Qa = team.Qa;
            existingTeam.Pmo = team.Pmo;

            try
            {
                _context.Teams.Update(existingTeam);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "اطلاعات تیم با موفقیت به‌روزرسانی شد.";
                return RedirectToAction("TeamList");
            }
            catch (Exception ex)
            {
                // Log the error (ex) here if you have logging configured
                TempData["ErrorMessage"] = "خطایی در به‌روزرسانی اطلاعات تیم رخ داد.";
                return View("EditTeam", team);
            }
        }


        [HttpPost]
        public async Task<IActionResult> DeleteTeam(Guid id)
        {
            var team = await _context.Teams.FindAsync(id);
            if (team == null)
            {
                TempData["ErrorMessage"] = "تیم مورد نظر یافت نشد.";
                return RedirectToAction("TeamList");
            }



            _context.Teams.Remove(team);  // Remove the user from the database
            await _context.SaveChangesAsync();  // Save the changes to the database

            TempData["SuccessMessage"] = "تیم با موفقیت حذف شد.";
            return RedirectToAction("TeamList");
        }





    }



}
