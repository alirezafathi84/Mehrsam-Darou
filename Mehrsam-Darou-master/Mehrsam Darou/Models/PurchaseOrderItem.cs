using System;
using System.Collections.Generic;

namespace Mehrsam_Darou.Models;

public partial class PurchaseOrderItem
{
    public Guid PoItemId { get; set; }

    public Guid PurchaseOrderId { get; set; }

    public Guid MaterialId { get; set; }

    public decimal Quantity { get; set; }

    public Guid UnitId { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal TotalPrice { get; set; }

    public decimal? ReceivedQuantity { get; set; }

    public string? Notes { get; set; }

    public virtual RawMaterial Material { get; set; } = null!;

    public virtual PurchaseOrder PurchaseOrder { get; set; } = null!;

    public virtual Unit Unit { get; set; } = null!;
}
