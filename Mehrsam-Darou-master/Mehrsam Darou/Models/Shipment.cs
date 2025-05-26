using System;
using System.Collections.Generic;

namespace Mehrsam_Darou.Models;

public partial class Shipment
{
    public Guid ShipmentId { get; set; }

    public string ShipmentNumber { get; set; } = null!;

    public Guid? SalesOrderId { get; set; }

    public Guid CustomerId { get; set; }

    public DateOnly ShipmentDate { get; set; }

    public DateOnly? ExpectedDeliveryDate { get; set; }

    public DateOnly? ActualDeliveryDate { get; set; }

    public string? Carrier { get; set; }

    public string? TrackingNumber { get; set; }

    public string? ShippingAddress { get; set; }

    public decimal? ShippingCost { get; set; }

    public decimal? WeightKg { get; set; }

    public decimal? VolumeM3 { get; set; }

    public string? Status { get; set; }

    public string? Notes { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime CreatedDate { get; set; }

    public virtual Customer Customer { get; set; } = null!;

    public virtual SalesOrder? SalesOrder { get; set; }

    public virtual ICollection<ShipmentItem> ShipmentItems { get; set; } = new List<ShipmentItem>();
}
