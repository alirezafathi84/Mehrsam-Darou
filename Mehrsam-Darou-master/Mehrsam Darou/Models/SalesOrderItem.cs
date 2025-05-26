using System;
using System.Collections.Generic;

namespace Mehrsam_Darou.Models;

public partial class SalesOrderItem
{
    public Guid SoItemId { get; set; }

    public Guid SalesOrderId { get; set; }

    public Guid MedicineId { get; set; }

    public decimal Quantity { get; set; }

    public Guid UnitId { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal? DiscountPercent { get; set; }

    public decimal TotalPrice { get; set; }

    public decimal? ShippedQuantity { get; set; }

    public string? Notes { get; set; }

    public virtual Medicine Medicine { get; set; } = null!;

    public virtual SalesOrder SalesOrder { get; set; } = null!;

    public virtual Unit Unit { get; set; } = null!;
}
