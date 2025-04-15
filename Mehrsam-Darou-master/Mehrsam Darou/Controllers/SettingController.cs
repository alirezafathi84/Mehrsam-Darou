using Mehrsam_Darou.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Mehrsam_Darou.Controllers
{
    public class SettingController : BaseController
    {
        private readonly ILogger<SettingController> _logger;
        private readonly DarouAppContext _context;

        public SettingController(DarouAppContext context, ILogger<SettingController> logger) : base(context)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Setting
        public async Task<IActionResult> Setting()
        {
            var user = await ValidateSessionAndGetUser();
            if (user == null)
            {
                return RedirectToAction("Login", "Client");
            }

            var settings = await _context.Settings.FirstOrDefaultAsync();
            if (settings == null)
            {
                settings = new Setting
                {
                    DefaultColor = false, // پیش‌فرض: تم روشن
                    IsNavDark = false,    // پیش‌فرض: نوار بالایی روشن
                    IsMenuDark = false,  // پیش‌فرض: منوی تاریک
                    NumberPerPage = 10   // پیش‌فرض: 10 آیتم در هر صفحه
                };
                _context.Settings.Add(settings);
                await _context.SaveChangesAsync();
            }

            ViewData["IsDark"] = settings.DefaultColor;
            ViewData["IsNavDark"] = settings.IsNavDark;
            ViewData["IsMenuDark"] = settings.IsMenuDark;

            return View("Setting", settings);
        }

        // GET: Setting/Edit
        public async Task<IActionResult> Edit()
        {


            var settings = await _context.Settings.FirstOrDefaultAsync();
            if (settings == null)
            {
                return NotFound();
            }

            ViewData["IsDark"] = settings.DefaultColor;
            ViewData["IsNavDark"] = settings.IsNavDark;
            ViewData["IsMenuDark"] = settings.IsMenuDark;

            return View("Setting", settings);
        }

        // POST: Setting/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Setting setting)
        {


            if (ModelState.IsValid)
            {
                try
                {
                    var existingSetting = await _context.Settings.FirstOrDefaultAsync();
                    if (existingSetting == null)
                    {
                        _context.Settings.Add(setting);
                    }
                    else
                    {
                        existingSetting.DefaultColor = setting.DefaultColor;
                        existingSetting.IsNavDark = setting.IsNavDark;
                        existingSetting.IsMenuDark = setting.IsMenuDark;
                        existingSetting.NumberPerPage = setting.NumberPerPage;



                        _context.Settings.Update(existingSetting);

                        var user1 = await ValidateSessionAndGetUser();
                        if (user1 == null)
                        {
                            return RedirectToAction("Login", "Client");
                        }


                    }

                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "تنظیمات با موفقیت به‌روزرسانی شد.";
                    return View("Setting", setting); // برگرداندن همان View با داده‌های به‌روز
                }
                catch (DbUpdateConcurrencyException)
                {
                    TempData["ErrorMessage"] = "خطا در به‌روزرسانی تنظیمات.";
                    return View("Setting", setting);
                }
            }

            TempData["ErrorMessage"] = "خطا در اعتبارسنجی داده‌ها.";
            return View("Setting", setting); // در صورت نامعتبر بودن مدل، همان View را با داده‌های فعلی برگردان
        }
    }
}