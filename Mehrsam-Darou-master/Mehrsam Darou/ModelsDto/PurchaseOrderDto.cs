using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Mehrsam_Darou.ModelsDto
{
    /// <summary>
    /// DTO برای ایجاد سفارش خرید جدید - تطابق با مدل‌های واقعی پروژه
    /// </summary>
    public class PurchaseOrderDto
    {
        public string PoNumber { get; set; } = null!;
        public DateOnly OrderDate { get; set; }
        public Guid SupplierId { get; set; }
        public DateOnly? ExpectedDeliveryDate { get; set; }
        public string? Currency { get; set; }
        public string? Status { get; set; }
        public string? Notes { get; set; }
        public decimal? TotalAmount { get; set; }
        public List<PurchaseOrderItemDto> Items { get; set; } = new List<PurchaseOrderItemDto>();
    }

    /// <summary>
    /// DTO برای اقلام سفارش خرید
    /// </summary>
    public class PurchaseOrderItemDto
    {
        public Guid MaterialId { get; set; }
        public Guid UnitId { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public string? Notes { get; set; }
    }
}


