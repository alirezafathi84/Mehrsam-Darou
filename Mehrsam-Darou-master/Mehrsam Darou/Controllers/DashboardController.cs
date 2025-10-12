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
            var thirtyDaysAgo = currentDate.AddDays(-30);

            try
            {
                // Load all dashboard data
                await LoadKPIData(model, startOfCurrentMonth, startOfLastMonth);
                await LoadChartData(model, startOfCurrentYear);
                await LoadRecentOrders(model);
                await LoadTopProducts(model, startOfCurrentMonth);
                await LoadTopCustomers(model, startOfCurrentMonth);
                await LoadInventoryStatus(model);
                await LoadProductionStatus(model, currentDate);
                await LoadQualityControlStatus(model);
                await LoadShipmentStatus(model, currentDate);
                await LoadFinancialStatus(model);
                await LoadMaterialRequestStatus(model, startOfCurrentMonth);
                await LoadProjectStatus(model);
                await LoadComplianceStatus(model, currentDate);
                await LoadRecentActivities(model);
                await LoadDashboardAlerts(model, currentDate);
            }
            catch (Exception ex)
            {
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

            var lastMonthProduction = await _context.ProductionOrders
                .Where(po => po.TargetDate >= DateOnly.FromDateTime(startOfLastMonth) &&
                            po.TargetDate < DateOnly.FromDateTime(startOfCurrentMonth))
                .CountAsync();

            model.TotalProductionOrders = currentMonthProduction;
            model.ProductionOrdersGrowth = lastMonthProduction > 0 ?
                Math.Round(((decimal)(currentMonthProduction - lastMonthProduction) / lastMonthProduction) * 100, 1) : 0;

            // Revenue
            var currentMonthRevenue = await _context.SalesInvoices
                .Where(si => si.CreatedDate >= startOfCurrentMonth)
                .SumAsync(si => si.TotalAmount);

            var lastMonthRevenue = await _context.SalesInvoices
                .Where(si => si.CreatedDate >= startOfLastMonth && si.CreatedDate < startOfCurrentMonth)
                .SumAsync(si => si.TotalAmount);

            model.TotalRevenue = currentMonthRevenue;
            model.RevenueGrowth = lastMonthRevenue > 0 ?
                Math.Round(((currentMonthRevenue - lastMonthRevenue) / lastMonthRevenue) * 100, 1) : 0;

            // Average Order Value
            model.AverageOrderValue = currentMonthOrders > 0 ?
                Math.Round(currentMonthRevenue / currentMonthOrders, 0) : 0;

            // Active Material Requests
            model.ActiveMaterialRequests = await _context.MaterialRequests
                .Where(mr => mr.Status != "تکمیل شده" && mr.Status != "لغو شده" && mr.IsActive == true)
                .CountAsync();

            // Pending Approvals
            model.PendingApprovals = await _context.RequestApprovals
                .Where(ra => ra.ApprovalStatus == "در انتظار" && ra.IsActive == true)
                .CountAsync();

            // Active Projects
            model.TotalActiveProjects = await _context.Projects
                .Where(p => p.Status == "فعال" && p.IsActive == true)
                .CountAsync();
        }

        private async Task LoadChartData(DashboardViewModel model, DateTime startOfCurrentYear)
        {
            var monthlySales = new List<int>();
            var monthlyProduction = new List<int>();
            var monthlyRevenue = new List<decimal>();

            for (int month = 1; month <= 12; month++)
            {
                var monthStart = new DateTime(startOfCurrentYear.Year, month, 1);
                var monthEnd = monthStart.AddMonths(1);

                // Sales count
                var salesCount = await _context.SalesOrders
                    .Where(so => so.CreatedDate >= monthStart && so.CreatedDate < monthEnd)
                    .CountAsync();
                monthlySales.Add(salesCount);

                // Production count
                var productionCount = await _context.ProductionOrders
                    .Where(po => po.TargetDate >= DateOnly.FromDateTime(monthStart) &&
                                po.TargetDate < DateOnly.FromDateTime(monthEnd))
                    .CountAsync();
                monthlyProduction.Add(productionCount);

                // Revenue
                var revenue = await _context.SalesInvoices
                    .Where(si => si.InvoiceDate >= DateOnly.FromDateTime(monthStart) &&
                                si.InvoiceDate < DateOnly.FromDateTime(monthEnd))
                    .SumAsync(si => si.TotalAmount);
                monthlyRevenue.Add(revenue);
            }

            model.MonthlySales = monthlySales;
            model.MonthlyProduction = monthlyProduction;
            model.MonthlyRevenue = monthlyRevenue;
        }

        private async Task LoadRecentOrders(DashboardViewModel model)
        {
            var recentOrdersData = await _context.SalesOrders
                .Include(so => so.Customer)
                .Include(so => so.SalesOrderItems)
                    .ThenInclude(soi => soi.Medicine)
                .OrderByDescending(so => so.CreatedDate)
                .Take(10)
                .Select(so => new
                {
                    OrderNumber = so.SoNumber,
                    OrderDate = so.CreatedDate,
                    CustomerName = so.Customer.CustomerName,
                    CustomerEmail = so.Customer.Email ?? "",
                    CustomerPhone = so.Customer.Phone ?? "",
                    TotalAmount = so.NetAmount ?? 0,
                    Status = so.Status ?? "نامشخص",
                    TopProductName = so.SalesOrderItems.FirstOrDefault().Medicine.BrandName ?? "",
                    DeliveryDate = so.PromisedDeliveryDate
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
                TopProductName = so.TopProductName,
                DaysUntilDelivery = so.DeliveryDate.HasValue ?
                    (so.DeliveryDate.Value.ToDateTime(TimeOnly.MinValue) - DateTime.Now).Days : 0
            }).ToList();

            model.RecentOrders = recentOrders;
        }

        private async Task LoadTopProducts(DashboardViewModel model, DateTime startOfMonth)
        {
            var topProducts = await _context.SalesOrderItems
                .Include(soi => soi.Medicine)
                    .ThenInclude(m => m.Category)
                .Include(soi => soi.SalesOrder)
                .Where(soi => soi.SalesOrder.CreatedDate >= startOfMonth)
                .GroupBy(soi => new {
                    soi.MedicineId,
                    soi.Medicine.BrandName,
                    soi.Medicine.MedicineCode,
                    soi.Medicine.Category.CategoryName
                })
                .Select(g => new TopProductViewModel
                {
                    MedicineName = g.Key.BrandName,
                    MedicineCode = g.Key.MedicineCode,
                    CategoryName = g.Key.CategoryName,
                    TotalSold = (int)g.Sum(soi => soi.Quantity),
                    Revenue = g.Sum(soi => soi.TotalPrice)
                })
                .OrderByDescending(tp => tp.Revenue)
                .Take(5)
                .ToListAsync();

            model.TopProducts = topProducts;
        }

        private async Task LoadTopCustomers(DashboardViewModel model, DateTime startOfMonth)
        {
            var topCustomers = await _context.SalesOrders
                .Include(so => so.Customer)
                .Where(so => so.CreatedDate >= startOfMonth)
                .GroupBy(so => new {
                    so.CustomerId,
                    so.Customer.CustomerName,
                    so.Customer.CustomerCode,
                    so.Customer.CustomerType
                })
                .Select(g => new TopCustomerViewModel
                {
                    CustomerName = g.Key.CustomerName,
                    CustomerCode = g.Key.CustomerCode,
                    CustomerType = g.Key.CustomerType,
                    TotalOrders = g.Count(),
                    TotalRevenue = g.Sum(so => so.NetAmount ?? 0),
                    LastOrderDate = g.Max(so => so.CreatedDate)
                })
                .OrderByDescending(tc => tc.TotalRevenue)
                .Take(5)
                .ToListAsync();

            model.TopCustomers = topCustomers;
        }

        private async Task LoadInventoryStatus(DashboardViewModel model)
        {
            // Low stock materials
            var lowStockMaterials = await _context.MaterialBatches
                .Include(mb => mb.Material)
                .Include(mb => mb.Unit)
                .Where(mb => mb.Material.MinStockLevel.HasValue &&
                            mb.CurrentQuantity <= mb.Material.MinStockLevel &&
                            mb.Status == "آزاد شده")
                .GroupBy(mb => new {
                    mb.MaterialId,
                    mb.Material.MaterialName,
                    mb.Material.MaterialCode,
                    mb.Material.MinStockLevel,
                    mb.Unit.UnitName
                })
                .Select(g => new
                {
                    g.Key.MaterialName,
                    g.Key.MaterialCode,
                    g.Key.MinStockLevel,
                    g.Key.UnitName,
                    CurrentStock = g.Sum(mb => mb.CurrentQuantity)
                })
                .ToListAsync();

            model.LowStockItems = lowStockMaterials.Count;
            model.CriticalStockItems = lowStockMaterials.Count(m => m.CurrentStock < m.MinStockLevel * 0.5m);

            model.CriticalInventoryItems = lowStockMaterials
                .Take(10)
                .Select(m => new InventoryStatusViewModel
                {
                    ItemName = m.MaterialName,
                    ItemCode = m.MaterialCode,
                    CurrentStock = m.CurrentStock,
                    MinStockLevel = m.MinStockLevel ?? 0,
                    UnitName = m.UnitName,
                    Status = m.CurrentStock < m.MinStockLevel * 0.5m ? "خطرناک" : "کم",
                    StatusColor = m.CurrentStock < m.MinStockLevel * 0.5m ? "danger" : "warning"
                })
                .ToList();

            model.TotalMedicines = await _context.Medicines.Where(m => m.IsActive == true).CountAsync();
            model.TotalRawMaterials = await _context.RawMaterials.Where(rm => rm.IsActive == true).CountAsync();
        }

        private async Task LoadProductionStatus(DashboardViewModel model, DateTime currentDate)
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

            // Delayed production orders
            model.DelayedProductionOrders = await _context.ProductionOrders
                .Where(po => po.Status != "تکمیل شده" &&
                            po.Status != "لغو شده" &&
                            po.TargetDate < DateOnly.FromDateTime(currentDate))
                .CountAsync();

            // Production efficiency
            var totalProduction = model.PendingProductionOrders + model.InProgressProductionOrders + model.CompletedProductionOrders;
            model.ProductionEfficiencyRate = totalProduction > 0 ?
                Math.Round((decimal)model.CompletedProductionOrders / totalProduction * 100, 1) : 0;
        }

        private async Task LoadQualityControlStatus(DashboardViewModel model)
        {
            model.PassedQCTests = await _context.BatchTests
                .Where(bt => bt.PassFailStatus == "قبول" && bt.IsActive == true)
                .CountAsync();

            model.FailedQCTests = await _context.BatchTests
                .Where(bt => bt.PassFailStatus == "رد" && bt.IsActive == true)
                .CountAsync();

            model.PendingQCTests = await _context.BatchTests
                .Where(bt => bt.TestStatus == "در حال انجام" || bt.TestStatus == "برنامه‌ریزی شده")
                .CountAsync();

            model.RetestRequired = await _context.BatchTests
                .Where(bt => bt.RetestRequired == true && bt.IsActive == true)
                .CountAsync();

            var totalTests = model.PassedQCTests + model.FailedQCTests;
            model.QCPassRate = totalTests > 0 ?
                Math.Round((decimal)model.PassedQCTests / totalTests * 100, 1) : 0;

            model.ConversionRate = (int)model.QCPassRate;
        }

        private async Task LoadShipmentStatus(DashboardViewModel model, DateTime currentDate)
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

            model.DelayedShipments = await _context.Shipments
                .Where(s => s.ExpectedDeliveryDate.HasValue &&
                           s.ExpectedDeliveryDate.Value < DateOnly.FromDateTime(currentDate) &&
                           s.Status != "تحویل داده شده")
                .CountAsync();

            // On-time delivery rate
            var deliveredWithDate = await _context.Shipments
                .Where(s => s.Status == "تحویل داده شده" &&
                           s.ExpectedDeliveryDate.HasValue &&
                           s.ActualDeliveryDate.HasValue)
                .ToListAsync();

            if (deliveredWithDate.Any())
            {
                var onTime = deliveredWithDate.Count(s => s.ActualDeliveryDate <= s.ExpectedDeliveryDate);
                model.OnTimeDeliveryRate = Math.Round((decimal)onTime / deliveredWithDate.Count * 100, 1);
            }
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

            // Collection efficiency
            var totalInvoiced = model.PendingInvoicesAmount + model.PaidInvoicesAmount + model.OverdueInvoicesAmount;
            model.CollectionEfficiency = totalInvoiced > 0 ?
                Math.Round(model.PaidInvoicesAmount / totalInvoiced * 100, 1) : 0;

            // Purchase data
            model.TotalPurchaseOrders = await _context.PurchaseOrders
                .Where(po => po.Status != "لغو شده")
                .CountAsync();

            model.TotalPurchaseAmount = await _context.PurchaseOrders
                .Where(po => po.Status != "لغو شده")
                .SumAsync(po => po.TotalAmount ?? 0);

            // Weekly comparisons
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

        private async Task LoadMaterialRequestStatus(DashboardViewModel model, DateTime startOfMonth)
        {
            model.TotalMaterialRequestsThisMonth = await _context.MaterialRequests
                .Where(mr => mr.RequestDate >= startOfMonth && mr.IsActive == true)
                .CountAsync();

            var completedRequests = await _context.MaterialRequests
                .Where(mr => mr.Status == "تکمیل شده" &&
                            mr.RequestDate >= startOfMonth.AddMonths(-1) &&
                            mr.IsActive == true)
                .CountAsync();

            var totalRequests = await _context.MaterialRequests
                .Where(mr => mr.RequestDate >= startOfMonth.AddMonths(-1) && mr.IsActive == true)
                .CountAsync();

            model.MaterialRequestFulfillmentRate = totalRequests > 0 ?
                Math.Round((decimal)completedRequests / totalRequests * 100, 1) : 0;

            // Fixed: Fetch data first, then get user information separately
            var pendingRequestsData = await _context.MaterialRequests
                .Where(mr => mr.Status != "تکمیل شده" &&
                            mr.Status != "لغو شده" &&
                            mr.IsActive == true)
                .OrderByDescending(mr => mr.PriorityLevel)
                .ThenBy(mr => mr.RequestDate)
                .Take(5)
                .Select(mr => new
                {
                    mr.RequestNumber,
                    mr.RequestTitle,
                    mr.RequestDate,
                    mr.RequestedBy,
                    mr.Status,
                    mr.PriorityLevel,
                    ItemCount = mr.MaterialRequestItems.Count,
                    mr.TotalEstimatedCost
                })
                .ToListAsync();

            // Get user information
            var userIds = pendingRequestsData.Select(r => r.RequestedBy).Distinct().ToList();
            var users = await _context.Users
                .Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.FirstName + " " + u.LastName);

            // Map to view model
            var pendingRequests = pendingRequestsData.Select(mr => new MaterialRequestSummaryViewModel
            {
                RequestNumber = mr.RequestNumber,
                RequestTitle = mr.RequestTitle,
                RequestDate = mr.RequestDate,
                RequestedBy = users.ContainsKey(mr.RequestedBy) ? users[mr.RequestedBy] : "نامشخص",
                Status = mr.Status,
                Priority = GetPriorityText(mr.PriorityLevel),
                ItemCount = mr.ItemCount,
                EstimatedCost = mr.TotalEstimatedCost ?? 0
            }).ToList();

            model.PendingMaterialRequests = pendingRequests;
        }

        private async Task LoadProjectStatus(DashboardViewModel model)
        {
            model.ActiveResearchProjects = await _context.ResearchProjects
                .Where(p => p.ProjectStatus == "در حال اجرا" && p.IsActive == true)
                .CountAsync();

            model.ActiveDevelopmentProjects = await _context.DevelopmentProjects
                .Where(p => p.ProjectStatus == "در حال اجرا" && p.IsActive == true)
                .CountAsync();

            var criticalProjectsData = await _context.Projects
                .Where(p => p.Status == "فعال" && p.IsActive == true)
                .OrderBy(p => p.EndDate)
                .Take(5)
                .ToListAsync();

            // Fixed: Removed duplicate assignment
            model.CriticalProjects = criticalProjectsData.Select(p => new ProjectStatusViewModel
            {
                ProjectCode = p.ProjectCode,
                ProjectName = p.ProjectName,
                Status = p.Status,
                TargetDate = p.EndDate.HasValue ? p.EndDate.Value.ToDateTime(TimeOnly.MinValue) : null,
                DaysRemaining = p.EndDate.HasValue ?
                    (p.EndDate.Value.ToDateTime(TimeOnly.MinValue) - DateTime.Now).Days : 0
            }).ToList();
        }



        private async Task LoadComplianceStatus(DashboardViewModel model, DateTime currentDate)
        {
            var thirtyDaysFromNow = currentDate.AddDays(30);

            model.ExpiringCertifications = await _context.Certifications
                .Where(c => c.ExpiryDate.HasValue &&
                           c.ExpiryDate.Value >= DateOnly.FromDateTime(currentDate) &&
                           c.ExpiryDate.Value <= DateOnly.FromDateTime(thirtyDaysFromNow) &&
                           c.IsActive == true)
                .CountAsync();

            model.UpcomingAudits = await _context.QaAudits
                .Where(a => a.PlannedStartDate.HasValue &&
                           a.PlannedStartDate.Value >= DateOnly.FromDateTime(currentDate) &&
                           a.PlannedStartDate.Value <= DateOnly.FromDateTime(thirtyDaysFromNow) &&
                           a.IsActive == true)
                .CountAsync();
        }

        // Fix for Error 2 - Line 540 (CreatedByNavigation)
        // Change the LoadRecentActivities method to this:

        private async Task LoadRecentActivities(DashboardViewModel model)
        {
            var recentActivities = new List<RecentActivityViewModel>();

            // Recent sales orders - Fixed to handle potential null CreatedBy
            var recentSalesData = await _context.SalesOrders
                .OrderByDescending(so => so.CreatedDate)
                .Take(3)
                .Select(so => new
                {
                    so.SoNumber,
                    so.CreatedDate,
                    so.CreatedBy
                })
                .ToListAsync();

            var salesUserIds = recentSalesData
                .Where(s => s.CreatedBy.HasValue)
                .Select(s => s.CreatedBy.Value)
                .Distinct()
                .ToList();

            var salesUsers = await _context.Users
                .Where(u => salesUserIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.FirstName + " " + u.LastName);

            var recentSales = recentSalesData.Select(so => new RecentActivityViewModel
            {
                ActivityType = "سفارش فروش",
                Description = $"سفارش {so.SoNumber} ثبت شد",
                ActivityDate = so.CreatedDate,
                UserName = so.CreatedBy.HasValue && salesUsers.ContainsKey(so.CreatedBy.Value)
                    ? salesUsers[so.CreatedBy.Value]
                    : "سیستم",
                IconClass = "bx-cart",
                ColorClass = "primary"
            }).ToList();

            // Recent material requests - Fixed similarly
            var recentRequestsData = await _context.MaterialRequests
                .OrderByDescending(mr => mr.CreatedDate)
                .Take(3)
                .Select(mr => new
                {
                    mr.RequestNumber,
                    mr.CreatedDate,
                    mr.CreatedBy
                })
                .ToListAsync();

            var requestUserIds = recentRequestsData
                .Select(r => r.CreatedBy)
                .Distinct()
                .ToList();

            var requestUsers = await _context.Users
                .Where(u => requestUserIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.FirstName + " " + u.LastName);

            var recentRequests = recentRequestsData.Select(mr => new RecentActivityViewModel
            {
                ActivityType = "درخواست مواد",
                Description = $"درخواست {mr.RequestNumber} ثبت شد",
                ActivityDate = mr.CreatedDate,
                UserName = requestUsers.ContainsKey(mr.CreatedBy)
                    ? requestUsers[mr.CreatedBy]
                    : "نامشخص",
                IconClass = "bx-package",
                ColorClass = "warning"
            }).ToList();

            recentActivities.AddRange(recentSales);
            recentActivities.AddRange(recentRequests);

            model.RecentActivities = recentActivities
                .OrderByDescending(a => a.ActivityDate)
                .Take(10)
                .ToList();
        }
        private async Task LoadDashboardAlerts(DashboardViewModel model, DateTime currentDate)
        {
            var alerts = new List<DashboardAlertViewModel>();

            // Critical inventory alerts
            if (model.CriticalStockItems > 0)
            {
                alerts.Add(new DashboardAlertViewModel
                {
                    AlertType = "خطر",
                    Title = "موجودی بحرانی",
                    Message = $"{model.CriticalStockItems} قلم موجودی در وضعیت بحرانی",
                    IconClass = "bx-error",
                    ColorClass = "danger",
                    ActionUrl = "/Inventory/MaterialBatches",
                    CreatedDate = currentDate
                });
            }

            // Delayed production
            if (model.DelayedProductionOrders > 0)
            {
                alerts.Add(new DashboardAlertViewModel
                {
                    AlertType = "هشدار",
                    Title = "تاخیر تولید",
                    Message = $"{model.DelayedProductionOrders} سفارش تولید با تاخیر",
                    IconClass = "bx-time",
                    ColorClass = "warning",
                    ActionUrl = "/PMO/ProductionOrders",
                    CreatedDate = currentDate
                });
            }

            // Overdue invoices
            if (model.OverdueInvoicesAmount > 0)
            {
                alerts.Add(new DashboardAlertViewModel
                {
                    AlertType = "هشدار",
                    Title = "فاکتورهای سررسید گذشته",
                    Message = $"{model.OverdueInvoicesAmount:N0} ریال فاکتور سررسید گذشته",
                    IconClass = "bx-receipt",
                    ColorClass = "warning",
                    ActionUrl = "/SellCommercial/SalesInvoices",
                    CreatedDate = currentDate
                });
            }

            // Expiring certifications
            if (model.ExpiringCertifications > 0)
            {
                alerts.Add(new DashboardAlertViewModel
                {
                    AlertType = "اطلاعات",
                    Title = "گواهینامه‌های در حال انقضا",
                    Message = $"{model.ExpiringCertifications} گواهینامه در 30 روز آینده منقضی می‌شود",
                    IconClass = "bx-certification",
                    ColorClass = "info",
                    ActionUrl = "/QA/Certifications",
                    CreatedDate = currentDate
                });
            }

            model.Alerts = alerts.OrderByDescending(a => a.CreatedDate).ToList();
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

        private static string GetPriorityText(int priorityLevel)
        {
            return priorityLevel switch
            {
                1 => "بحرانی",
                2 => "بالا",
                3 => "متوسط",
                4 => "پایین",
                5 => "کم اهمیت",
                _ => "متوسط"
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
            model.MonthlyRevenue = Enumerable.Repeat(0m, 12).ToList();
            model.ConversionRate = 0;
        }
    }
}