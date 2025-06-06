using Mehrsam_Darou.Models;
using Mehrsam_Darou.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Mehrsam_Darou.Helper.Helper;
using System.Globalization;

namespace Mehrsam_Darou.Controllers
{
    public class DashboardController : BaseController
    {
        private readonly DarouAppContext _context;
        private readonly PersianCalendar _persianCalendar;

        public DashboardController(DarouAppContext context) : base(context)
        {
            _context = context;
            _persianCalendar = new PersianCalendar();
        }

        public async Task<IActionResult> Dashboard()
        {
            var model = new DashboardViewModel();

            // Calculate date ranges
            var currentDate = DateTime.Now;
            var startOfCurrentMonth = new DateTime(currentDate.Year, currentDate.Month, 1);
            var startOfLastMonth = startOfCurrentMonth.AddMonths(-1);
            var startOfCurrentYear = new DateTime(currentDate.Year, 1, 1);

            try
            {
                // KPI Data
                await LoadKPIData(model, startOfCurrentMonth, startOfLastMonth);

                // Chart Data - Monthly performance for current year
                await LoadChartData(model, startOfCurrentYear);

                // Recent Orders
                await LoadRecentOrders(model);

                // Top Products
                await LoadTopProducts(model);

                // Inventory Status
                await LoadInventoryStatus(model);

                // Production Status
                await LoadProductionStatus(model);

                // Quality Control Status
                await LoadQualityControlStatus(model);

                // Shipment Status
                await LoadShipmentStatus(model);

                // Financial Status
                await LoadFinancialStatus(model);
            }
            catch (Exception ex)
            {
                // Log error and provide default values
                Console.WriteLine($"Dashboard error: {ex.Message}");
                SetDefaultValues(model);
            }

            return View(model);
        }

        private async Task LoadKPIData(DashboardViewModel model, DateTime startOfCurrentMonth, DateTime startOfLastMonth)
        {
            // Sales Orders
            var currentMonthOrders = await _context.SalesOrders
                .Where(so => so.CreatedDate >= startOfCurrentMonth)
                .CountAsync();
            
            var lastMonthOrders = await _context.SalesOrders
                .Where(so => so.CreatedDate >= startOfLastMonth && so.CreatedDate < startOfCurrentMonth)
                .CountAsync();

            model.TotalSalesOrders = currentMonthOrders;
            model.SalesOrdersGrowth = lastMonthOrders > 0 ? 
                Math.Round(((decimal)(currentMonthOrders - lastMonthOrders) / lastMonthOrders) * 100, 1) : 0;

            // Customers
            var currentMonthCustomers = await _context.Customers
                .Where(c => c.CreatedDate >= startOfCurrentMonth)
                .CountAsync();
            
            var lastMonthCustomers = await _context.Customers
                .Where(c => c.CreatedDate >= startOfLastMonth && c.CreatedDate < startOfCurrentMonth)
                .CountAsync();

            model.TotalCustomers = await _context.Customers.Where(c => c.IsActive == true).CountAsync();
            model.CustomersGrowth = lastMonthCustomers > 0 ? 
                Math.Round(((decimal)(currentMonthCustomers - lastMonthCustomers) / lastMonthCustomers) * 100, 1) : 0;

            // Production Orders
            var currentMonthProduction = await _context.ProductionOrders
                .Where(po => po.TargetDate >= DateOnly.FromDateTime(startOfCurrentMonth))
                .CountAsync();

            model.TotalProductionOrders = currentMonthProduction;

            // Revenue (from sales invoices)
            var currentMonthRevenue = await _context.SalesInvoices
                .Where(si => si.CreatedDate >= startOfCurrentMonth)
                .SumAsync(si => si.TotalAmount);

            var lastMonthRevenue = await _context.SalesInvoices
                .Where(si => si.CreatedDate >= startOfLastMonth && si.CreatedDate < startOfCurrentMonth)
                .SumAsync(si => si.TotalAmount);

            model.TotalRevenue = currentMonthRevenue;
            model.RevenueGrowth = lastMonthRevenue > 0 ? 
                Math.Round(((currentMonthRevenue - lastMonthRevenue) / lastMonthRevenue) * 100, 1) : 0;
        }

        private async Task LoadChartData(DashboardViewModel model, DateTime startOfCurrentYear)
        {
            var monthlySales = new List<int>();
            var monthlyProduction = new List<int>();

            for (int month = 1; month <= 12; month++)
            {
                var monthStart = new DateTime(startOfCurrentYear.Year, month, 1);
                var monthEnd = monthStart.AddMonths(1);

                // Sales data
                var salesCount = await _context.SalesOrders
                    .Where(so => so.CreatedDate >= monthStart && so.CreatedDate < monthEnd)
                    .CountAsync();
                monthlySales.Add(salesCount);

                // Production data
                var productionCount = await _context.ProductionOrders
                    .Where(po => po.TargetDate >= DateOnly.FromDateTime(monthStart) && 
                                po.TargetDate < DateOnly.FromDateTime(monthEnd))
                    .CountAsync();
                monthlyProduction.Add(productionCount);
            }

            model.MonthlySales = monthlySales;
            model.MonthlyProduction = monthlyProduction;
        }

        private async Task LoadRecentOrders(DashboardViewModel model)
        {
            var recentOrdersData = await _context.SalesOrders
                .Include(so => so.Customer)
                .Include(so => so.SalesOrderItems)
                    .ThenInclude(soi => soi.Medicine)
                .OrderByDescending(so => so.CreatedDate)
                .Take(5)
                .Select(so => new
                {
                    OrderNumber = so.SoNumber,
                    OrderDate = so.CreatedDate,
                    CustomerName = so.Customer.CustomerName,
                    CustomerEmail = so.Customer.Email ?? "",
                    CustomerPhone = so.Customer.Phone ?? "",
                    TotalAmount = so.NetAmount ?? 0,
                    Status = so.Status ?? "نامشخص",
                    TopProductName = so.SalesOrderItems.FirstOrDefault().Medicine.BrandName ?? ""
                })
                .ToListAsync();

            var recentOrders = recentOrdersData.Select(so => new RecentOrderViewModel
            {
                OrderNumber = so.OrderNumber,
                OrderDate = so.OrderDate,
                CustomerName = so.CustomerName,
                CustomerEmail = so.CustomerEmail,
                CustomerPhone = so.CustomerPhone,
                TotalAmount = so.TotalAmount,
                Status = so.Status,
                StatusColor = GetStatusColor(so.Status),
                TopProductName = so.TopProductName
            }).ToList();

            model.RecentOrders = recentOrders;
        }

        private async Task LoadTopProducts(DashboardViewModel model)
        {
            var topProducts = await _context.SalesOrderItems
                .Include(soi => soi.Medicine)
                    .ThenInclude(m => m.Category)
                .GroupBy(soi => new { soi.MedicineId, soi.Medicine.BrandName, soi.Medicine.MedicineCode, soi.Medicine.Category.CategoryName })
                .Select(g => new TopProductViewModel
                {
                    MedicineName = g.Key.BrandName,
                    MedicineCode = g.Key.MedicineCode,
                    CategoryName = g.Key.CategoryName,
                    TotalSold = (int)g.Sum(soi => soi.Quantity),
                    Revenue = g.Sum(soi => soi.TotalPrice)
                })
                .OrderByDescending(tp => tp.TotalSold)
                .Take(5)
                .ToListAsync();

            model.TopProducts = topProducts;
        }

        private async Task LoadInventoryStatus(DashboardViewModel model)
        {
            // Low stock raw materials
            model.LowStockItems = await _context.MaterialBatches
                .Include(mb => mb.Material)
                .Where(mb => mb.Material.MinStockLevel.HasValue && 
                            mb.CurrentQuantity <= mb.Material.MinStockLevel)
                .CountAsync();

            model.TotalMedicines = await _context.Medicines.Where(m => m.IsActive == true).CountAsync();
            model.TotalRawMaterials = await _context.RawMaterials.Where(rm => rm.IsActive == true).CountAsync();
        }

        private async Task LoadProductionStatus(DashboardViewModel model)
        {
            model.PendingProductionOrders = await _context.ProductionOrders
                .Where(po => po.Status == "برنامه‌ریزی شده")
                .CountAsync();

            model.InProgressProductionOrders = await _context.ProductionOrders
                .Where(po => po.Status == "در حال اجرا")
                .CountAsync();

            model.CompletedProductionOrders = await _context.ProductionOrders
                .Where(po => po.Status == "تکمیل شده")
                .CountAsync();
        }

        private async Task LoadQualityControlStatus(DashboardViewModel model)
        {
            // These would be based on your QC implementation
            // For now, using production orders as proxy
            var totalProduction = await _context.ProductionOrders.CountAsync();
            model.PassedQCTests = (int)(totalProduction * 0.85); // 85% pass rate
            model.FailedQCTests = (int)(totalProduction * 0.10); // 10% fail rate
            model.PendingQCTests = totalProduction - model.PassedQCTests - model.FailedQCTests;

            // Calculate conversion rate for quality
            model.ConversionRate = totalProduction > 0 ? 
                (model.PassedQCTests * 100 / totalProduction) : 0;
        }

        private async Task LoadShipmentStatus(DashboardViewModel model)
        {
            model.PendingShipments = await _context.Shipments
                .Where(s => s.Status == "در حال آماده‌سازی" || s.Status == "آماده")
                .CountAsync();

            model.InTransitShipments = await _context.Shipments
                .Where(s => s.Status == "ارسال شده" || s.Status == "در حال حمل")
                .CountAsync();

            model.DeliveredShipments = await _context.Shipments
                .Where(s => s.Status == "تحویل داده شده")
                .CountAsync();
        }

        private async Task LoadFinancialStatus(DashboardViewModel model)
        {
            model.PendingInvoicesAmount = await _context.SalesInvoices
                .Where(si => si.Status == "پیش‌نویس" || si.Status == "ارسال شده")
                .SumAsync(si => si.TotalAmount);

            model.PaidInvoicesAmount = await _context.SalesInvoices
                .Where(si => si.Status == "پرداخت شده")
                .SumAsync(si => si.TotalAmount);

            model.OverdueInvoicesAmount = await _context.SalesInvoices
                .Where(si => si.Status == "سررسید گذشته")
                .SumAsync(si => si.TotalAmount);

            // Weekly comparisons for conversions
            var thisWeekStart = DateTime.Now.AddDays(-(int)DateTime.Now.DayOfWeek);
            var lastWeekStart = thisWeekStart.AddDays(-7);

            var thisWeekOrders = await _context.SalesOrders
                .Where(so => so.CreatedDate >= thisWeekStart)
                .CountAsync();

            var lastWeekOrders = await _context.SalesOrders
                .Where(so => so.CreatedDate >= lastWeekStart && so.CreatedDate < thisWeekStart)
                .CountAsync();

            model.ThisWeekConversions = thisWeekOrders.ToString("N0");
            model.LastWeekConversions = lastWeekOrders.ToString("N0");
        }

        private static string GetStatusColor(string status)
        {
            return status switch
            {
                "تکمیل شده" or "تحویل داده شده" or "پرداخت شده" => "success",
                "در حال اجرا" or "در حال حمل" or "ارسال شده" => "primary",
                "لغو شده" or "برگشت داده شده" => "danger",
                "سررسید گذشته" => "warning",
                _ => "secondary"
            };
        }

        private void SetDefaultValues(DashboardViewModel model)
        {
            model.TotalSalesOrders = 0;
            model.TotalCustomers = 0;
            model.TotalProductionOrders = 0;
            model.TotalRevenue = 0;
            model.MonthlySales = Enumerable.Repeat(0, 12).ToList();
            model.MonthlyProduction = Enumerable.Repeat(0, 12).ToList();
            model.ConversionRate = 0;
        }
    }
}