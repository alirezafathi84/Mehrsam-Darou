using Mehrsam_Darou.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using static Mehrsam_Darou.Helper.Helper;
using System.Text.Json;
using System.Globalization;

public class BaseController : Controller
{
    protected readonly DarouAppContext _context;

    public BaseController(DarouAppContext context)
    {
        _context = context;
    }

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

    protected async Task SetCommonViewData(User user, Guid companyGuid)
    {
        var setting = await ReadSettingAsync(_context);
        ViewData["IsDark"] = setting?.DefaultColor == true;
        ViewData["IsNavDark"] = setting?.IsNavDark == true;
        ViewData["IsMenuDark"] = setting?.IsMenuDark == true;

        ViewData["Fullname"] = " " + user.FirstName + " " + user.LastName;
        ViewData["Avatar"] = user.AvatarImg == null ? "\\images\\users\\dummy-avatar.jpg" : user.AvatarImg;

        // Fetch the company name
        var company = await _context.Organizations.FirstOrDefaultAsync(u => u.Id == companyGuid);
        ViewData["Company"] = company?.Name ?? "Unknown Company";
    }

    protected async Task<Setting> ReadSettingAsync(DarouAppContext context)
    {
        return await context.Settings.FirstOrDefaultAsync();
    }

    private bool VerifyPassword1(string hashedPassword, string providedPassword)
    {
        return VerifyPassword(hashedPassword, providedPassword);
    }

    protected Guid? GetCurrentUserId()
    {
        var username = HttpContext.Session.GetString("Username");
        if (!string.IsNullOrEmpty(username))
        {
            var user = _context.Users.FirstOrDefault(u => u.Username == username);
            return user?.Id;
        }
        return null;
    }

    #region Menu State Management

    [HttpPost]
    public async Task<IActionResult> SaveMenuState([FromBody] MenuStateModel model)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Json(new { success = false, message = "کاربر یافت نشد" });
            }

            // Validate input
            if (model?.ExpandedMenus == null)
            {
                return Json(new { success = false, message = "داده‌های ورودی نامعتبر" });
            }

            // Create a unique session key for menu state
            var sessionKey = $"MenuState_{userId}";

            // Serialize the expanded menus to JSON
            var menuStateJson = JsonSerializer.Serialize(model.ExpandedMenus);

            // Save to session with user-specific key
            HttpContext.Session.SetString(sessionKey, menuStateJson);

            // Also save a general key for backward compatibility
            HttpContext.Session.SetString("MenuState", menuStateJson);

            return Json(new
            {
                success = true,
                message = "وضعیت منو با موفقیت ذخیره شد",
                savedMenus = model.ExpandedMenus,
                timestamp = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")
            });
        }
        catch (JsonException ex)
        {
            return Json(new { success = false, message = $"خطا در سریال‌سازی داده‌ها: {ex.Message}" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"خطا در ذخیره وضعیت منو: {ex.Message}" });
        }
    }

    [HttpGet]
    public IActionResult GetMenuState()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Json(new
                {
                    success = false,
                    expandedMenus = new string[] { },
                    message = "کاربر یافت نشد"
                });
            }

            // Try to get user-specific menu state first
            var sessionKey = $"MenuState_{userId}";
            var menuStateJson = HttpContext.Session.GetString(sessionKey);

            // Fall back to general menu state if user-specific doesn't exist
            if (string.IsNullOrEmpty(menuStateJson))
            {
                menuStateJson = HttpContext.Session.GetString("MenuState");
            }

            var expandedMenus = new string[] { };

            if (!string.IsNullOrEmpty(menuStateJson))
            {
                try
                {
                    expandedMenus = JsonSerializer.Deserialize<string[]>(menuStateJson) ?? new string[] { };
                }
                catch (JsonException ex)
                {
                    // If deserialization fails, return empty array but log the error
                    expandedMenus = new string[] { };
                    // You might want to log this error: Logger.LogWarning($"Failed to deserialize menu state: {ex.Message}");
                }
            }

            return Json(new
            {
                success = true,
                expandedMenus = expandedMenus,
                timestamp = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"),
                userId = userId.ToString()
            });
        }
        catch (Exception ex)
        {
            return Json(new
            {
                success = false,
                expandedMenus = new string[] { },
                message = $"خطا در بازیابی وضعیت منو: {ex.Message}"
            });
        }
    }

    [HttpPost]
    public async Task<IActionResult> ClearMenuState()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Json(new { success = false, message = "کاربر یافت نشد" });
            }

            // Clear user-specific menu state
            var sessionKey = $"MenuState_{userId}";
            HttpContext.Session.Remove(sessionKey);
            HttpContext.Session.Remove("MenuState");

            return Json(new
            {
                success = true,
                message = "وضعیت منو با موفقیت پاک شد",
                timestamp = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")
            });
        }
        catch (Exception ex)
        {
            return Json(new
            {
                success = false,
                message = $"خطا در پاک کردن وضعیت منو: {ex.Message}"
            });
        }
    }

    #endregion

    #region Theme Settings Management

    [HttpPost]
    public async Task<IActionResult> SaveThemeSettings([FromBody] ThemeSettingsModel model)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Json(new { success = false, message = "کاربر یافت نشد" });
            }

            var settings = await _context.Settings.FirstOrDefaultAsync(s => s.Id == userId);

            if (settings == null)
            {
                settings = new Setting
                {
                    Id = userId.Value,
                    NumberPerPage = 10,
                    DefaultColor = false,
                    IsNavDark = false,
                    IsMenuDark = false
                };
                _context.Settings.Add(settings);
            }

            // Apply theme settings
            switch (model.ThemeMode?.ToLower())
            {
                case "dark":
                    settings.DefaultColor = true;
                    break;
                case "light":
                    settings.DefaultColor = false;
                    break;
            }

            switch (model.TopbarColor?.ToLower())
            {
                case "dark":
                    settings.IsNavDark = true;
                    break;
                case "light":
                    settings.IsNavDark = false;
                    break;
            }

            switch (model.MenuColor?.ToLower())
            {
                case "dark":
                    settings.IsMenuDark = true;
                    break;
                case "light":
                    settings.IsMenuDark = false;
                    break;
            }

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = "تنظیمات تم با موفقیت ذخیره شد",
                appliedSettings = new
                {
                    themeMode = settings.DefaultColor == true ? "dark" : "light",
                    topbarColor = settings.IsNavDark == true ? "dark" : "light",
                    menuColor = settings.IsMenuDark == true ? "dark" : "light"
                }
            });
        }
        catch (Exception ex)
        {
            return Json(new
            {
                success = false,
                message = $"خطا در ذخیره تنظیمات تم: {ex.Message}"
            });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetThemeSettings()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Json(new
                {
                    success = false,
                    message = "کاربر یافت نشد"
                });
            }

            var settings = await _context.Settings.FirstOrDefaultAsync(s => s.Id == userId);

            if (settings == null)
            {
                return Json(new
                {
                    success = true,
                    settings = new
                    {
                        themeMode = "light",
                        topbarColor = "light",
                        menuColor = "light"
                    }
                });
            }

            return Json(new
            {
                success = true,
                settings = new
                {
                    themeMode = settings.DefaultColor == true ? "dark" : "light",
                    topbarColor = settings.IsNavDark == true ? "dark" : "light",
                    menuColor = settings.IsMenuDark == true ? "dark" : "light"
                }
            });
        }
        catch (Exception ex)
        {
            return Json(new
            {
                success = false,
                message = $"خطا در بازیابی تنظیمات تم: {ex.Message}"
            });
        }
    }

    #endregion

    protected async Task SetUserPermissions(User user)
    {
        Team t = _context.Teams.SingleOrDefault(t => t.Id.Equals(user.TeamId));
        if (t != null)
        {
            ViewData["ManagmentMenu"] = t.ManagmentDashboard == true;
            ViewData["SettingMenu"] = t.Setting == true;
            ViewData["SystemUsersMenu"] = t.SystemUsers == true;
            ViewData["FinancialMenu"] = t.Financial == true;
            ViewData["InventoryMenu"] = t.Inventory == true;
            ViewData["ProductMenu"] = t.Product == true;
            ViewData["SellCommercialMenu"] = t.SellCommercial == true;
            ViewData["BuyCommercialMenu"] = t.BuyCommercial == true;
            ViewData["RandDMenu"] = t.RandD == true;
            ViewData["QcMenu"] = t.Qc == true;
            ViewData["QaMenu"] = t.Qa == true;
            ViewData["PmoMenu"] = t.Pmo == true;
        }
        else
        {
            ViewData["ManagmentMenu"] = false;
            ViewData["SettingMenu"] = false;
            ViewData["SystemUsersMenu"] = false;
            ViewData["FinancialMenu"] = false;
            ViewData["InventoryMenu"] = false;
            ViewData["ProductMenu"] = false;
            ViewData["SellCommercialMenu"] = false;
            ViewData["BuyCommercialMenu"] = false;
            ViewData["RandDMenu"] = false;
            ViewData["QcMenu"] = false;
            ViewData["QaMenu"] = false;
            ViewData["PmoMenu"] = false;
        }
    }

    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var actionName = context.ActionDescriptor.RouteValues["action"];
        var controllerName = context.ActionDescriptor.RouteValues["controller"];

        // Skip authentication for login and utility actions
        if (controllerName == "Client" && actionName == "Login" ||
            (controllerName == "Base" && (actionName == "SaveMenuState" ||
                                        actionName == "GetMenuState" ||
                                        actionName == "ClearMenuState" ||
                                        actionName == "SaveThemeSettings" ||
                                        actionName == "GetThemeSettings")))
        {
            await next();
            return;
        }

        var user = await ValidateSessionAndGetUser();

        if (user == null)
        {
            // For AJAX requests, return JSON error
            if (context.HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                context.HttpContext.Request.ContentType?.Contains("application/json") == true)
            {
                context.Result = Json(new
                {
                    success = false,
                    message = "جلسه کاری منقضی شده است",
                    redirect = "/Client/Login"
                });
                return;
            }

            context.Result = new RedirectToActionResult("Login", "Client", null);
            return;
        }

        // Add UserId to ViewData for all views
        ViewData["UserId"] = user.Id.ToString();

        // Set user permissions
        await SetUserPermissions(user);

        // Load activity logs
        var logEntries = await _context.UserEnterLogs
            .Include(log => log.User)
                .ThenInclude(u => u.Team)
            .OrderByDescending(log => log.CreatedDate)
            .Take(50)
            .ToListAsync();

        ViewData["LogEntries"] = logEntries;

        // Load settings with proper null handling
        var setting = await ReadSettingAsync(_context);
        ViewData["IsDark"] = setting?.DefaultColor == true;
        ViewData["IsNavDark"] = setting?.IsNavDark == true;
        ViewData["IsMenuDark"] = setting?.IsMenuDark == true;

        // Load notifications
        var unreadCount = await _context.Notifications
            .Where(n => !n.Seen && (n.UserId == user.Id || n.UserId == null))
            .CountAsync();

        var notifications = await _context.Notifications
            .Where(n => (n.UserId == user.Id || n.UserId == null) && !n.Seen)
            .OrderByDescending(n => n.CreatedDate)
            .Take(10)
            .ToListAsync();

        // Get recent notifications (last 7 days) for notification dropdown
        var recentNotifications = await _context.Notifications
            .Where(n => (n.UserId == user.Id || n.UserId == null) &&
                       n.CreatedDate >= DateTime.Now.AddDays(-7))
            .OrderByDescending(n => n.CreatedDate)
            .Take(15)
            .ToListAsync();

        // Get notification statistics for dashboard
        var todayNotifications = await _context.Notifications
            .Where(n => (n.UserId == user.Id || n.UserId == null) &&
                       n.CreatedDate.Date == DateTime.Today)
            .CountAsync();

        var weekNotifications = await _context.Notifications
            .Where(n => (n.UserId == user.Id || n.UserId == null) &&
                       n.CreatedDate >= DateTime.Now.AddDays(-7))
            .CountAsync();

        // Group notifications by type for better organization
        var notificationsByType = notifications.GroupBy(n => n.Type ?? "General")
            .ToDictionary(g => g.Key, g => g.Count());

        ViewData["UnreadNotificationsCount"] = unreadCount;
        ViewData["Notifications"] = notifications;
        ViewData["RecentNotifications"] = recentNotifications;
        ViewData["TodayNotificationsCount"] = todayNotifications;
        ViewData["WeekNotificationsCount"] = weekNotifications;
        ViewData["NotificationsByType"] = notificationsByType;

        await next();
    }
}

#region Data Models

public class MenuStateModel
{
    public string[] ExpandedMenus { get; set; } = new string[] { };
}

public class ThemeSettingsModel
{
    public string ThemeMode { get; set; } = "light";
    public string TopbarColor { get; set; } = "light";
    public string MenuColor { get; set; } = "light";
    public string MenuSize { get; set; } = "default";
}

#endregion