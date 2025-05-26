using System;
using System.Collections.Generic;

namespace Mehrsam_Darou.Models;

public partial class SalesOrder
{
    public Guid SalesOrderId { get; set; }

    public string SoNumber { get; set; } = null!;

    public Guid CustomerId { get; set; }

    public DateOnly OrderDate { get; set; }

    public DateOnly? RequestedDeliveryDate { get; set; }

    public DateOnly? PromisedDeliveryDate { get; set; }

    public decimal? TotalAmount { get; set; }

    public decimal? TaxAmount { get; set; }

    public decimal? DiscountAmount { get; set; }

    public decimal? NetAmount { get; set; }

    public string? Currency { get; set; }

    public string? Status { get; set; }

    public int? Priority { get; set; }

    public string? Notes { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime CreatedDate { get; set; }

    public virtual Customer Customer { get; set; } = null!;

    public virtual ICollection<SalesInvoice> SalesInvoices { get; set; } = new List<SalesInvoice>();

    public virtual ICollection<SalesOrderItem> SalesOrderItems { get; set; } = new List<SalesOrderItem>();

    public virtual ICollection<Shipment> Shipments { get; set; } = new List<Shipment>();
}
