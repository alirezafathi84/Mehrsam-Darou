using System;
using System.Collections.Generic;

namespace Mehrsam_Darou.Models;

public partial class PurchaseOrder
{
    public Guid PurchaseOrderId { get; set; }

    public string PoNumber { get; set; } = null!;

    public Guid SupplierId { get; set; }

    public DateOnly OrderDate { get; set; }

    public DateOnly? ExpectedDeliveryDate { get; set; }

    public DateOnly? ActualDeliveryDate { get; set; }

    public decimal? TotalAmount { get; set; }

    public string? Currency { get; set; }

    public string? Status { get; set; }

    public string? Notes { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime CreatedDate { get; set; }

    public virtual ICollection<MaterialRequestItem> MaterialRequestItems { get; set; } = new List<MaterialRequestItem>();

    public virtual ICollection<PurchaseInvoice> PurchaseInvoices { get; set; } = new List<PurchaseInvoice>();

    public virtual ICollection<PurchaseOrderItem> PurchaseOrderItems { get; set; } = new List<PurchaseOrderItem>();

    public virtual Supplier Supplier { get; set; } = null!;
}
