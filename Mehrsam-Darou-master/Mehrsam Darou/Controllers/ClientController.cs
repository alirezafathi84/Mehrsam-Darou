using Mehrsam_Darou.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Mehrsam_Darou.Helper.Helper;
namespace Mehrsam_Darou.Controllers
{
    public class ClientController : Controller
    {
        private readonly ILogger<ClientController> _logger;
        private readonly DarouAppContext _context;

        public ClientController(DarouAppContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Login()
        {
            // Retrieve settings for theme and pagination
            var setting = await ReadSettingAsync(_context);

            // Set the theme (dark or light) based on settings
            ViewData["IsDark"] = setting?.DefaultColor ?? false;


            // Fetch organizations and pass them to the view
            var orgs = await _context.Organizations.OrderBy(e => e.Priority).ToListAsync();
            return View(orgs);  // Directly passing the organizations list to the view
        }


        [HttpPost]
        public async Task<IActionResult> LoginByUserPass(string username, string password, string OrgDDL)
        {
            // Clear the session in case of a failed login attempt
            HttpContext.Session.Clear();

            // Retrieve the user from the database based on the username
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);

            if (user == null || !Helper.Helper.VerifyPassword(user.Password, password))
            {
                // If user does not exist or password does not match, show an error message
                ViewBag.ErrorMessage = "نام کاربری و یا کلمه عبور نادرست است";

                // Fetch organizations and pass them to the view again
                var orgs = await _context.Organizations.OrderBy(e => e.Priority).ToListAsync();
                return View("Login", orgs); // Passing the org list back to the view
            }

            // Save session information if login is successful
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("Password", password);
            HttpContext.Session.SetString("UserId", user.Id.ToString());
            // Set the selected organization ID
            HttpContext.Session.SetString("CompanyGuid", OrgDDL);

            UserEnterLog eul = new UserEnterLog();
            eul.CreatedDate = DateTime.Now;
            eul.UserId = user.Id;
            eul.Status = "In";
            AddUserEnterLog(eul);

            return RedirectToAction("UserList", "User");
        }


        [HttpPost]
        public IActionResult Logout()
        {
            UserEnterLog eul = new UserEnterLog();
            eul.CreatedDate = DateTime.Now;
            eul.UserId = new Guid(HttpContext.Session.GetString("UserId"));
            eul.Status = "Out";
            AddUserEnterLog(eul);

            // Clear the session
            HttpContext.Session.Clear();

            // Optionally, clear cookies if needed
            foreach (var cookie in Request.Cookies.Keys)
            {
                Response.Cookies.Delete(cookie);
            }

            // Redirect to the login page
            return RedirectToAction("Login", "Client");
        }



        public async Task<IActionResult> AddUserEnterLog(UserEnterLog UserEnterLog)
        {
            _context.UserEnterLogs.Add(UserEnterLog);
            await _context.SaveChangesAsync();
            return View();
        }
    }
}
