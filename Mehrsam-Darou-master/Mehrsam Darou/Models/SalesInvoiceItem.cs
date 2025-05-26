using System;
using System.Collections.Generic;

namespace Mehrsam_Darou.Models;

public partial class SalesInvoiceItem
{
    public Guid SiItemId { get; set; }

    public Guid SalesInvoiceId { get; set; }

    public Guid MedicineId { get; set; }

    public Guid? BatchId { get; set; }

    public decimal Quantity { get; set; }

    public Guid UnitId { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal? DiscountPercent { get; set; }

    public decimal TotalPrice { get; set; }

    public string? Notes { get; set; }

    public virtual FinishedGoodsBatch? Batch { get; set; }

    public virtual Medicine Medicine { get; set; } = null!;

    public virtual SalesInvoice SalesInvoice { get; set; } = null!;

    public virtual Unit Unit { get; set; } = null!;
}
