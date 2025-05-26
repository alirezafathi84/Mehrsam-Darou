using System;
using System.Collections.Generic;

namespace Mehrsam_Darou.Models;

public partial class SalesInvoice
{
    public Guid SalesInvoiceId { get; set; }

    public string InvoiceNumber { get; set; } = null!;

    public Guid? SalesOrderId { get; set; }

    public Guid CustomerId { get; set; }

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

    public virtual Customer Customer { get; set; } = null!;

    public virtual ICollection<SalesInvoiceItem> SalesInvoiceItems { get; set; } = new List<SalesInvoiceItem>();

    public virtual SalesOrder? SalesOrder { get; set; }
}
