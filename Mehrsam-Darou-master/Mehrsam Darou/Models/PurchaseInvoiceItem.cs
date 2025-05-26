using System;
using System.Collections.Generic;

namespace Mehrsam_Darou.Models;

public partial class PurchaseInvoiceItem
{
    public Guid PiItemId { get; set; }

    public Guid PurchaseInvoiceId { get; set; }

    public Guid MaterialId { get; set; }

    public Guid? BatchId { get; set; }

    public decimal Quantity { get; set; }

    public Guid UnitId { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal TotalPrice { get; set; }

    public string? Notes { get; set; }

    public virtual MaterialBatch? Batch { get; set; }

    public virtual RawMaterial Material { get; set; } = null!;

    public virtual PurchaseInvoice PurchaseInvoice { get; set; } = null!;

    public virtual Unit Unit { get; set; } = null!;
}
