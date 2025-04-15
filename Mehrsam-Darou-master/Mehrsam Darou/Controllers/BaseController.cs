using Mehrsam_Darou.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using static Mehrsam_Darou.Helper.Helper;

public class BaseController : Controller
{
    protected readonly DarouAppContext _context;

    public BaseController(DarouAppContext context)
    {
        _context = context;
    }
    /// <summary>
    /// Validates the user session and retrieves user information.
    /// </summary>
    /// <returns>
    /// Returns the authenticated user if the session is valid; otherwise, returns null.
    /// </returns>
    protected async Task<User> ValidateSessionAndGetUser()
    {
        var username = HttpContext.Session.GetString("Username");
        var password = HttpContext.Session.GetString("Password");
        Guid companyGuid = Guid.TryParse(HttpContext.Session.GetString("CompanyGuid"), out Guid parsedGuid) ? parsedGuid : Guid.Empty;

        // Check if session values are empty
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) || companyGuid == Guid.Empty)
        {
            return null;
        }

        // Fetch the user from the database
        var user = await _context.Users.SingleOrDefaultAsync(u => u.Username == username);

        // Verify the user and password
        if (user == null || !VerifyPassword1(user.Password, password))
        {
            return null;
        }

        // Set common view data (e.g., theme, full name, company name)
        await SetCommonViewData(user, companyGuid);

        return user;
    }

    /// <summary>
    /// Sets common view data for the layout (e.g., theme, full name, company name).
    /// </summary>
    protected async Task SetCommonViewData(User user, Guid companyGuid)
    {
        var setting = await ReadSettingAsync(_context);
        ViewData["IsDark"] = setting?.DefaultColor ?? false;
        ViewData["IsNavDark"] = setting?.IsNavDark ?? false;
        ViewData["IsMenuDark"] = setting?.IsMenuDark ?? false;

        ViewData["Fullname"] = " " + user.FirstName + " " + user.LastName;
        ViewData["Avatar"] = user.AvatarImg == null ? "\\images\\users\\dummy-avatar.jpg" : user.AvatarImg;   //"\\images\\users\\avatar-7.jpg";
        // Fetch the company name
        var company = await _context.Organizations.FirstOrDefaultAsync(u => u.Id == companyGuid);
        ViewData["Company"] = company?.Name ?? "Unknown Company";
    }

    /// <summary>
    /// Reads application settings from the database.
    /// </summary>
    private async Task<Setting> ReadSettingAsync(DarouAppContext context)
    {
        return await context.Settings.FirstOrDefaultAsync();
    }

    /// <summary>
    /// Verifies the user's password.
    /// </summary>
    private bool VerifyPassword1(string hashedPassword, string providedPassword)
    {
        // Implement your password verification logic here
        return VerifyPassword(hashedPassword, providedPassword);
    }

    /// <summary>
    /// This method is executed before every action in the controller to ensure the user is authenticated.
    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // Get the current action and controller names
        var actionName = context.ActionDescriptor.RouteValues["action"];
        var controllerName = context.ActionDescriptor.RouteValues["controller"];

        // Skip the redirect for the Login action in the Client controller
        if (controllerName == "Client" && actionName == "Login")
        {
            await next(); // Continue to the action without redirecting
            return;
        }

        // Validate the session for all other actions
        var user = await ValidateSessionAndGetUser();

        if (user == null)
        {
            // Redirect to login if the user is not authenticated
            context.Result = new RedirectToActionResult("Login", "Client", null);
            return;
        }

        var logEntries = await _context.UserEnterLogs
    .Include(log => log.User) // Include the related User data
    .OrderByDescending(log => log.CreatedDate)
    .Take(10) // Limit to the last 10 entries
    .ToListAsync();

        // Pass the log entries to the layout
        ViewData["LogEntries"] = logEntries;

        var setting = await ReadSettingAsync(_context);
        ViewData["IsDark"] = setting?.DefaultColor ?? false;
        ViewData["IsNavDark"] = setting?.IsNavDark ?? false;
        ViewData["IsMenuDark"] = setting?.IsMenuDark ?? false;


        Team t = new Team();
        t = _context.Teams.SingleOrDefault(t => t.Id.Equals(user.TeamId));
        ViewData["ManagmentMenu"] = t.ManagmentDashboard;
        ViewData["SettingMenu"] = t.Setting;
        ViewData["SystemUsersMenu"] = t.SystemUsers;
        ViewData["FinancialMenu"] = t.Financial;
        ViewData["InventoryMenu"] = t.Inventory;
        ViewData["ProductMenu"] = t.Product;
        ViewData["SellCommercialMenu"] = t.SellCommercial;
        ViewData["BuyCommercialMenu"] = t.BuyCommercial;
        ViewData["RandDMenu"] = t.RandD;
        ViewData["QcMenu"] = t.Qc;
        ViewData["QaMenu"] = t.Qa;
        ViewData["PmoMenu"] = t.Pmo;







        // Continue to the action if the user is authenticated
        await next();
    }


}
