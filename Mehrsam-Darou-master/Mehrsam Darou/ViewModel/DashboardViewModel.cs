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

        // Chart Data
        public List<int> MonthlySales { get; set; } = new List<int>();
        public List<int> MonthlyProduction { get; set; } = new List<int>();
        public List<string> PersianMonths { get; set; } = new List<string>
        {
            "فروردین", "اردیبهشت", "خرداد", "تیر", "مرداد", "شهریور",
            "مهر", "آبان", "آذر", "دی", "بهمن", "اسفند"
        };

        // Inventory Status
        public int LowStockItems { get; set; }
        public int TotalMedicines { get; set; }
        public int TotalRawMaterials { get; set; }

        // Production Status
        public int PendingProductionOrders { get; set; }
        public int CompletedProductionOrders { get; set; }
        public int InProgressProductionOrders { get; set; }

        // Quality Control
        public int PendingQCTests { get; set; }
        public int PassedQCTests { get; set; }
        public int FailedQCTests { get; set; }

        // Shipments
        public int PendingShipments { get; set; }
        public int DeliveredShipments { get; set; }
        public int InTransitShipments { get; set; }

        // Financial
        public decimal PendingInvoicesAmount { get; set; }
        public decimal PaidInvoicesAmount { get; set; }
        public decimal OverdueInvoicesAmount { get; set; }

        // Recent Orders
        public List<RecentOrderViewModel> RecentOrders { get; set; } = new List<RecentOrderViewModel>();

        // Top Products
        public List<TopProductViewModel> TopProducts { get; set; } = new List<TopProductViewModel>();

        // Conversion Chart Data (for quality/production efficiency)
        public int ConversionRate { get; set; }
        public string ConversionLabel { get; set; } ="QC";
        public string[] ConversionColors { get; set; } = new[] { "#ff6c2f", "#22c55e" };

        // Weekly comparisons
        public string ThisWeekConversions { get; set; } = "0";
        public string LastWeekConversions { get; set; } = "0";
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
    }

    public class TopProductViewModel
    {
        public string MedicineName { get; set; }
        public string MedicineCode { get; set; }
        public int TotalSold { get; set; }
        public decimal Revenue { get; set; }
        public string CategoryName { get; set; }
    }

    public class InventoryStatusViewModel
    {
        public string ItemName { get; set; }
        public string ItemCode { get; set; }
        public decimal CurrentStock { get; set; }
        public decimal MinStockLevel { get; set; }
        public string UnitName { get; set; }
        public string Status { get; set; } // "خطرناک", "کم", "عادی"
        public string StatusColor { get; set; }
    }
}