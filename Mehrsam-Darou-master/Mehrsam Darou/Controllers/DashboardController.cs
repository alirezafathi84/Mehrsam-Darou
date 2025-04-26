using Mehrsam_Darou.Models;
using Mehrsam_Darou.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Mehrsam_Darou.Helper.Helper;

namespace Mehrsam_Darou.Controllers
{
    public class DashboardController : BaseController
    {
        private readonly DarouAppContext _context;

        public DashboardController(DarouAppContext context) : base(context)
        {
            _context = context;
        }



        //public async Task<IActionResult> Dashboard(int? page, string SearchKey)
        //{
        //    // Example data - replace with your actual data logic
        //    var performanceData = new
        //    {
        //        Months = new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" },
        //        Values = new[] { 100, 120, 90, 150, 200, 180, 220, 250, 210, 300, 280, 350 }
        //    };

        //    // Option 1: Pass data directly to view
        //    ViewBag.ChartData = performanceData;

        //    // Or Option 2: Return as JSON for AJAX
        //    // return Json(performanceData);

        //    return View();
        //}

        //public IActionResult Dashboard()
        //{
        //    // Your data from database or other sources
        //    var performanceData = new
        //    {
        //        PageViews = new[] { 91, 65, 46, 68, 49, 61, 42, 44, 78, 52, 63, 67 },
        //        Clicks = new[] { 8, 12, 7, 17, 21, 11, 5, 9, 7, 29, 12, 35 },
        //        PersianMonths = new[] {
        //    "فروردین", "اردیبهشت", "خرداد", "تیر", "مرداد", "شهریور",
        //    "مهر", "آبان", "آذر", "دی", "بهمن", "اسفند"
        //}
        //    };

        //    return View(performanceData);
        //}

        public IActionResult Dashboard()
        {
            var model = new DashboardViewModel
            {
                ConversionRate = 63.2,
                ConversionLabel = "بازگشت خریدارها",
                ConversionColors = new[] { "#ff6c2f", "#22c55e" },
                ThisWeekConversions = "48.5k",
                LastWeekConversions = "41.05k",

                PageViews = new List<int> { 91, 65, 46, 68, 49, 61, 42, 44, 78, 52, 63, 67 },
                Clicks = new List<int> { 8, 12, 7, 17, 21, 11, 5, 9, 7, 29, 12, 35 },
                PersianMonths = new List<string>
        {
            "فروردین", "اردیبهشت", "خرداد", "تیر", "مرداد", "شهریور",
            "مهر", "آبان", "آذر", "دی", "بهمن", "اسفند"
        }
            };

            return View(model);
        }

    }
}
