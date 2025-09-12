using Mehrsam_Darou.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using static Mehrsam_Darou.Helper.Helper;
using System.Text.Json;
using System.Globalization;
// Add this using for Persian calendar
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

    #region Persian Date APIs

    [HttpPost]
    public JsonResult ConvertPersianDate([FromBody] PersianDateRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.PersianDate))
                return Json(new { success = false, error = "تاریخ نمی‌تواند خالی باشد" });

            // Parse Persian date (format: 1403/05/15)
            var persianDateClean = request.PersianDate
                .Replace("۰", "0").Replace("۱", "1").Replace("۲", "2")
                .Replace("۳", "3").Replace("۴", "4").Replace("۵", "5")
                .Replace("۶", "6").Replace("۷", "7").Replace("۸", "8")
                .Replace("۹", "9");

            var parts = persianDateClean.Split('/');

            if (parts.Length != 3)
                return Json(new { success = false, error = "فرمت تاریخ صحیح نیست" });

            if (!int.TryParse(parts[0], out int year) ||
                !int.TryParse(parts[1], out int month) ||
                !int.TryParse(parts[2], out int day))
            {
                return Json(new { success = false, error = "فرمت تاریخ صحیح نیست" });
            }

            // Validate Persian date ranges
            if (year < 1300 || year > 1500)
                return Json(new { success = false, error = "سال وارد شده معتبر نیست" });
            if (month < 1 || month > 12)
                return Json(new { success = false, error = "ماه وارد شده معتبر نیست" });
            if (day < 1 || day > 31)
                return Json(new { success = false, error = "روز وارد شده معتبر نیست" });

            // Convert using Persian Calendar
            var persianCalendar = new PersianCalendar();

            try
            {
                // Get current server time to preserve time portion
                var serverNow = DateTime.Now;

                // Persian Calendar months are 0-based in .NET, but our input is 1-based
                var gregorianDate = persianCalendar.ToDateTime(year, month, day,
                    serverNow.Hour, serverNow.Minute, serverNow.Second, serverNow.Millisecond);

                return Json(new
                {
                    success = true,
                    gregorianDate = gregorianDate.ToString("yyyy-MM-ddTHH:mm:ss.fff"),
                    serverTime = serverNow.ToString("HH:mm:ss")
                });
            }
            catch (ArgumentOutOfRangeException)
            {
                // Handle invalid dates like 31st day of a 30-day month
                return Json(new { success = false, error = "تاریخ وارد شده در تقویم شمسی معتبر نیست" });
            }
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = "خطا در تبدیل تاریخ: " + ex.Message });
        }
    }

    [HttpGet]
    public JsonResult GetTodayPersian()
    {
        try
        {
            var persianCalendar = new PersianCalendar();
            var serverNow = DateTime.Now; // Use server local time, not UTC

            var year = persianCalendar.GetYear(serverNow);
            var month = persianCalendar.GetMonth(serverNow);
            var day = persianCalendar.GetDayOfMonth(serverNow);

            return Json(new
            {
                success = true,
                year = year,
                month = month,
                day = day,
                formatted = $"{year:0000}/{month:00}/{day:00}",
                serverDateTime = serverNow.ToString("yyyy-MM-ddTHH:mm:ss.fff"),
                serverTime = serverNow.ToString("HH:mm:ss")
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
    }

    // Add new endpoint for Gregorian to Persian conversion
    [HttpPost]
    public JsonResult ConvertGregorianDate([FromBody] GregorianDateRequest request)
    {
        try
        {
            if (request.Year < 1900 || request.Year > 2200)
                return Json(new { success = false, error = "سال میلادی معتبر نیست" });

            if (request.Month < 1 || request.Month > 12)
                return Json(new { success = false, error = "ماه میلادی معتبر نیست" });

            if (request.Day < 1 || request.Day > 31)
                return Json(new { success = false, error = "روز میلادی معتبر نیست" });

            // Create DateTime with explicit UTC to avoid local timezone issues
            var utcDate = new DateTime(request.Year, request.Month, request.Day, 12, 0, 0, DateTimeKind.Utc);

            // Convert to local server time
            var localDate = utcDate.ToLocalTime();

            // Get current server time for time portion
            var serverNow = DateTime.Now;
            var finalDate = new DateTime(request.Year, request.Month, request.Day,
                serverNow.Hour, serverNow.Minute, serverNow.Second, serverNow.Millisecond, DateTimeKind.Local);

            var persianCalendar = new PersianCalendar();
            var persianYear = persianCalendar.GetYear(finalDate);
            var persianMonth = persianCalendar.GetMonth(finalDate);
            var persianDay = persianCalendar.GetDayOfMonth(finalDate);

            return Json(new
            {
                success = true,
                persianDate = new
                {
                    year = persianYear,
                    month = persianMonth,
                    day = persianDay
                },
                gregorianDate = finalDate.ToString("yyyy-MM-ddTHH:mm:ss.fff"),
                originalRequest = $"{request.Year}/{request.Month}/{request.Day}",
                serverTime = serverNow.ToString("HH:mm:ss"),
                debugInfo = new
                {
                    requestedDate = $"{request.Year}-{request.Month:00}-{request.Day:00}",
                    createdDateTime = finalDate.ToString("yyyy-MM-dd HH:mm:ss"),
                    persianResult = $"{persianYear}/{persianMonth}/{persianDay}"
                }
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = "خطا در تبدیل تاریخ میلادی: " + ex.Message });
        }
    }

    #endregion

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
        Team t = await _context.Teams.SingleOrDefaultAsync(t => t.Id.Equals(user.TeamId));
        if (t != null)
        {
            // Module-level permissions
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

            // Page-level permissions for Management Dashboard
            ViewData["ManagementDashboard_Dashboard"] = t.ManagementDashboardDashboard == true;
            ViewData["ManagementDashboard_Notifications"] = t.ManagementDashboardNotifications == true;
            ViewData["ManagementDashboard_AllRequests"] = t.ManagementDashboardAllRequests == true;
            ViewData["ManagementDashboard_RequestsDashboard"] = t.ManagementDashboardRequestsDashboard == true;

            // Page-level permissions for System Users
            ViewData["SystemUsers_UserList"] = t.SystemUsersUserList == true;
            ViewData["SystemUsers_TeamManagement"] = t.SystemUsersTeamManagement == true;

            // Page-level permissions for HR
            ViewData["HR_AttendanceLog"] = t.HrAttendanceLog == true;
            ViewData["HR_DailyAttendance"] = t.HrDailyAttendance == true;
            ViewData["HR_SalaryManagement"] = t.HrSalaryManagement == true;
            ViewData["HR_Vacations"] = t.HrVacations == true;
            ViewData["HR_VacationTypes"] = t.HrVacationTypes == true;
            ViewData["HR_SalaryCalculation"] = t.HrSalaryCalculation == true;

            // Page-level permissions for Product
            ViewData["Product_Medicines"] = t.ProductMedicines == true;
            ViewData["Product_MedicineCategories"] = t.ProductMedicineCategories == true;
            ViewData["Product_RawMaterials"] = t.ProductRawMaterials == true;
            ViewData["Product_MaterialCategories"] = t.ProductMaterialCategories == true;
            ViewData["Product_BOM"] = t.ProductBom == true;

            // Page-level permissions for Buy Commercial
            ViewData["BuyCommercial_Suppliers"] = t.BuyCommercialSuppliers == true;
            ViewData["BuyCommercial_PurchaseOrders"] = t.BuyCommercialPurchaseOrders == true;
            ViewData["BuyCommercial_PurchaseInvoices"] = t.BuyCommercialPurchaseInvoices == true;

            // Page-level permissions for Inventory
            ViewData["Inventory_StorageLocations"] = t.InventoryStorageLocations == true;
            ViewData["Inventory_MaterialBatches"] = t.InventoryMaterialBatches == true;
            ViewData["Inventory_FinishedGoodsBatches"] = t.InventoryFinishedGoodsBatches == true;

            // Page-level permissions for PMO
            ViewData["PMO_ProductionOrders"] = t.PmoProductionOrders == true;
            ViewData["PMO_ProductionSteps"] = t.PmoProductionSteps == true;

            // Page-level permissions for R&D
            ViewData["RandD_ResearchProjects"] = t.RandDResearchProjects == true;
            ViewData["RandD_Development"] = t.RandDDevelopment == true;
            ViewData["RandD_Formulas"] = t.RandDFormulas == true;

            // Page-level permissions for QC
            ViewData["QC_QCTests"] = t.QcQctests == true;
            ViewData["QC_BatchTests"] = t.QcBatchTests == true;
            ViewData["QC_QCReports"] = t.QcQcreports == true;

            // Page-level permissions for QA
            ViewData["QA_QAStandards"] = t.QaQastandards == true;
            ViewData["QA_QAAudits"] = t.QaQaaudits == true;
            ViewData["QA_Certifications"] = t.QaCertifications == true;

            // Page-level permissions for Sell Commercial
            ViewData["SellCommercial_Customers"] = t.SellCommercialCustomers == true;
            ViewData["SellCommercial_SalesOrders"] = t.SellCommercialSalesOrders == true;
            ViewData["SellCommercial_SalesInvoices"] = t.SellCommercialSalesInvoices == true;
            ViewData["SellCommercial_Shipments"] = t.SellCommercialShipments == true;

            // Page-level permissions for Financial
            ViewData["Financial_FinancialReports"] = t.FinancialFinancialReports == true;
            ViewData["Financial_Payments"] = t.FinancialPayments == true;
            ViewData["Financial_Accounting"] = t.FinancialAccounting == true;

            // Page-level permissions for Communication (always available)
            ViewData["Communication_Chat"] = t.CommunicationChat == true;
            ViewData["Communication_MyNotifications"] = t.CommunicationMyNotifications == true;
            ViewData["Communication_MyRequests"] = t.CommunicationMyRequests == true;

            // Page-level permissions for Settings
            ViewData["Setting_GeneralSettings"] = t.SettingGeneralSettings == true;
            ViewData["Setting_Organizations"] = t.SettingOrganizations == true;
            ViewData["Setting_Units"] = t.SettingUnits == true;
            ViewData["Setting_UnitTypes"] = t.SettingUnitTypes == true;
            ViewData["Setting_PersianDateConverter"] = t.SettingPersianDateConverter == true;
        }
        else
        {
            // Module-level permissions
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

            // Page-level permissions - all false when no team
            ViewData["ManagementDashboard_Dashboard"] = false;
            ViewData["ManagementDashboard_Notifications"] = false;
            ViewData["ManagementDashboard_AllRequests"] = false;
            ViewData["ManagementDashboard_RequestsDashboard"] = false;
            ViewData["SystemUsers_UserList"] = false;
            ViewData["SystemUsers_TeamManagement"] = false;
            ViewData["HR_AttendanceLog"] = false;
            ViewData["HR_DailyAttendance"] = false;
            ViewData["HR_SalaryManagement"] = false;
            ViewData["HR_Vacations"] = false;
            ViewData["HR_VacationTypes"] = false;
            ViewData["HR_SalaryCalculation"] = false;
            ViewData["Product_Medicines"] = false;
            ViewData["Product_MedicineCategories"] = false;
            ViewData["Product_RawMaterials"] = false;
            ViewData["Product_MaterialCategories"] = false;
            ViewData["Product_BOM"] = false;
            ViewData["BuyCommercial_Suppliers"] = false;
            ViewData["BuyCommercial_PurchaseOrders"] = false;
            ViewData["BuyCommercial_PurchaseInvoices"] = false;
            ViewData["Inventory_StorageLocations"] = false;
            ViewData["Inventory_MaterialBatches"] = false;
            ViewData["Inventory_FinishedGoodsBatches"] = false;
            ViewData["PMO_ProductionOrders"] = false;
            ViewData["PMO_ProductionSteps"] = false;
            ViewData["RandD_ResearchProjects"] = false;
            ViewData["RandD_Development"] = false;
            ViewData["RandD_Formulas"] = false;
            ViewData["QC_QCTests"] = false;
            ViewData["QC_BatchTests"] = false;
            ViewData["QC_QCReports"] = false;
            ViewData["QA_QAStandards"] = false;
            ViewData["QA_QAAudits"] = false;
            ViewData["QA_Certifications"] = false;
            ViewData["SellCommercial_Customers"] = false;
            ViewData["SellCommercial_SalesOrders"] = false;
            ViewData["SellCommercial_SalesInvoices"] = false;
            ViewData["SellCommercial_Shipments"] = false;
            ViewData["Financial_FinancialReports"] = false;
            ViewData["Financial_Payments"] = false;
            ViewData["Financial_Accounting"] = false;
            ViewData["Communication_Chat"] = false;
            ViewData["Communication_MyNotifications"] = false;
            ViewData["Communication_MyRequests"] = false;
            ViewData["Setting_GeneralSettings"] = false;
            ViewData["Setting_Organizations"] = false;
            ViewData["Setting_Units"] = false;
            ViewData["Setting_UnitTypes"] = false;
            ViewData["Setting_PersianDateConverter"] = false;
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
                                        actionName == "GetThemeSettings" ||
                                        actionName == "ConvertPersianDate" ||
                                        actionName == "GetTodayPersian" ||
                                        actionName == "ConvertGregorianDate")))
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

public class PersianDateRequest
{
    public string PersianDate { get; set; }
}

public class GregorianDateRequest
{
    public int Year { get; set; }
    public int Month { get; set; }
    public int Day { get; set; }
}

#endregion