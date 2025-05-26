using System;
using System.Collections.Generic;

namespace Mehrsam_Darou.Models;

public partial class PurchaseInvoice
{
    public Guid PurchaseInvoiceId { get; set; }

    public string InvoiceNumber { get; set; } = null!;

    public string? SupplierInvoiceNumber { get; set; }

    public Guid? PurchaseOrderId { get; set; }

    public Guid SupplierId { get; set; }

    public DateOnly InvoiceDate { get; set; }

    public DateOnly? DueDate { get; set; }

    public decimal Subtotal { get; set; }

    public decimal? TaxAmount { get; set; }

    public decimal? DiscountAmount { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal? PaidAmount { get; set; }

    public string? Currency { get; set; }

    public string? Status { get; set; }

    public string? PaymentMethod { get; set; }

    public string? Notes { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime CreatedDate { get; set; }

    public virtual ICollection<PurchaseInvoiceItem> PurchaseInvoiceItems { get; set; } = new List<PurchaseInvoiceItem>();

    public virtual PurchaseOrder? PurchaseOrder { get; set; }

    public virtual Supplier Supplier { get; set; } = null!;
}
