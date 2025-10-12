using System.ComponentModel.DataAnnotations;

namespace Mehrsam_Darou.ViewModel
{
    public class DashboardViewModel
    {
        // KPI Cards Data
        public int TotalSalesOrders { get; set; }
        public decimal SalesOrdersGrowth { get; set; }
        public int TotalCustomers { get; set; }
        public decimal CustomersGrowth { get; set; }
        public int TotalProductionOrders { get; set; }
        public decimal ProductionOrdersGrowth { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal RevenueGrowth { get; set; }

        // Additional KPIs
        public int ActiveMaterialRequests { get; set; }
        public int PendingApprovals { get; set; }
        public decimal AverageOrderValue { get; set; }
        public int TotalActiveProjects { get; set; }

        // Chart Data
        public List<int> MonthlySales { get; set; } = new List<int>();
        public List<int> MonthlyProduction { get; set; } = new List<int>();
        public List<decimal> MonthlyRevenue { get; set; } = new List<decimal>();
        public List<string> PersianMonths { get; set; } = new List<string>
        {
            "فروردین", "اردیبهشت", "خرداد", "تیر", "مرداد", "شهریور",
            "مهر", "آبان", "آذر", "دی", "بهمن", "اسفند"
        };

        // Inventory Status
        public int LowStockItems { get; set; }
        public int CriticalStockItems { get; set; }
        public int TotalMedicines { get; set; }
        public int TotalRawMaterials { get; set; }
        public List<InventoryStatusViewModel> CriticalInventoryItems { get; set; } = new List<InventoryStatusViewModel>();
        public decimal InventoryTurnoverRate { get; set; }

        // Production Status
        public int PendingProductionOrders { get; set; }
        public int CompletedProductionOrders { get; set; }
        public int InProgressProductionOrders { get; set; }
        public decimal ProductionEfficiencyRate { get; set; }
        public int DelayedProductionOrders { get; set; }

        // Quality Control
        public int PendingQCTests { get; set; }
        public int PassedQCTests { get; set; }
        public int FailedQCTests { get; set; }
        public decimal QCPassRate { get; set; }
        public int RetestRequired { get; set; }

        // Shipments
        public int PendingShipments { get; set; }
        public int DeliveredShipments { get; set; }
        public int InTransitShipments { get; set; }
        public int DelayedShipments { get; set; }
        public decimal OnTimeDeliveryRate { get; set; }

        // Financial
        public decimal PendingInvoicesAmount { get; set; }
        public decimal PaidInvoicesAmount { get; set; }
        public decimal OverdueInvoicesAmount { get; set; }
        public decimal CollectionEfficiency { get; set; }
        public int TotalPurchaseOrders { get; set; }
        public decimal TotalPurchaseAmount { get; set; }

        // Recent Activities
        public List<RecentOrderViewModel> RecentOrders { get; set; } = new List<RecentOrderViewModel>();
        public List<RecentActivityViewModel> RecentActivities { get; set; } = new List<RecentActivityViewModel>();

        // Top Products & Customers
        public List<TopProductViewModel> TopProducts { get; set; } = new List<TopProductViewModel>();
        public List<TopCustomerViewModel> TopCustomers { get; set; } = new List<TopCustomerViewModel>();

        // Material Requests
        public List<MaterialRequestSummaryViewModel> PendingMaterialRequests { get; set; } = new List<MaterialRequestSummaryViewModel>();
        public int TotalMaterialRequestsThisMonth { get; set; }
        public decimal MaterialRequestFulfillmentRate { get; set; }

        // R&D Projects
        public int ActiveResearchProjects { get; set; }
        public int ActiveDevelopmentProjects { get; set; }
        public List<ProjectStatusViewModel> CriticalProjects { get; set; } = new List<ProjectStatusViewModel>();

        // Compliance & Certifications
        public int ExpiringCertifications { get; set; }
        public int UpcomingAudits { get; set; }
        public int OpenDeviations { get; set; }

        // Conversion Chart Data
        public int ConversionRate { get; set; }
        public string ConversionLabel { get; set; } = "QC";
        public string[] ConversionColors { get; set; } = new[] { "#ff6c2f", "#22c55e" };

        // Weekly comparisons
        public string ThisWeekConversions { get; set; } = "0";
        public string LastWeekConversions { get; set; } = "0";

        // Alerts & Notifications
        public List<DashboardAlertViewModel> Alerts { get; set; } = new List<DashboardAlertViewModel>();
    }

    public class RecentOrderViewModel
    {
        public string OrderNumber { get; set; }
        public DateTime OrderDate { get; set; }
        public string CustomerName { get; set; }
        public string CustomerEmail { get; set; }
        public string CustomerPhone { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; }
        public string StatusColor { get; set; }
        public string TopProductName { get; set; }
        public int DaysUntilDelivery { get; set; }
    }

    public class TopProductViewModel
    {
        public string MedicineName { get; set; }
        public string MedicineCode { get; set; }
        public int TotalSold { get; set; }
        public decimal Revenue { get; set; }
        public string CategoryName { get; set; }
        public decimal GrowthPercentage { get; set; }
    }

    public class TopCustomerViewModel
    {
        public string CustomerName { get; set; }
        public string CustomerCode { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public DateTime LastOrderDate { get; set; }
        public string CustomerType { get; set; }
    }

    public class InventoryStatusViewModel
    {
        public string ItemName { get; set; }
        public string ItemCode { get; set; }
        public decimal CurrentStock { get; set; }
        public decimal MinStockLevel { get; set; }
        public string UnitName { get; set; }
        public string Status { get; set; }
        public string StatusColor { get; set; }
        public int DaysUntilStockout { get; set; }
        public bool HasPendingOrder { get; set; }
    }

    public class MaterialRequestSummaryViewModel
    {
        public string RequestNumber { get; set; }
        public string RequestTitle { get; set; }
        public DateTime RequestDate { get; set; }
        public string RequestedBy { get; set; }
        public string Status { get; set; }
        public string Priority { get; set; }
        public int ItemCount { get; set; }
        public decimal EstimatedCost { get; set; }
    }

    public class ProjectStatusViewModel
    {
        public string ProjectCode { get; set; }
        public string ProjectName { get; set; }
        public string Status { get; set; }
        public decimal CompletionPercentage { get; set; }
        public DateTime? TargetDate { get; set; }  // Changed from DateOnly? to DateTime?
        public int DaysRemaining { get; set; }
        public string RiskLevel { get; set; }
    }

    public class RecentActivityViewModel
    {
        public string ActivityType { get; set; }
        public string Description { get; set; }
        public DateTime ActivityDate { get; set; }
        public string UserName { get; set; }
        public string IconClass { get; set; }
        public string ColorClass { get; set; }
    }

    public class DashboardAlertViewModel
    {
        public string AlertType { get; set; } // "خطر", "هشدار", "اطلاعات"
        public string Title { get; set; }
        public string Message { get; set; }
        public string IconClass { get; set; }
        public string ColorClass { get; set; }
        public string ActionUrl { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}