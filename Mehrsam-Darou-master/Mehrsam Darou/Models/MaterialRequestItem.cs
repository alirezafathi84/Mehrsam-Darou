using System;
using System.Collections.Generic;

namespace Mehrsam_Darou.Models;

public partial class MaterialRequestItem
{
    public Guid ItemId { get; set; }

    public Guid RequestId { get; set; }

    public string ItemType { get; set; } = null!;

    public Guid? MaterialId { get; set; }

    public string ItemName { get; set; } = null!;

    public string? ItemDescription { get; set; }

    public string? Specification { get; set; }

    public string? BrandPreferred { get; set; }

    public string? ModelNumber { get; set; }

    public decimal QuantityRequested { get; set; }

    public Guid? UnitId { get; set; }

    public decimal? UnitPriceEstimated { get; set; }

    public decimal? TotalPriceEstimated { get; set; }

    public decimal? QuantityApproved { get; set; }

    public decimal? QuantityDelivered { get; set; }

    public DateTime? DeliveryDate { get; set; }

    public Guid? SupplierId { get; set; }

    public Guid? PurchaseOrderId { get; set; }

    public Guid? SubstituteMaterialId { get; set; }

    public string? SubstituteNotes { get; set; }

    public string ItemStatus { get; set; } = null!;

    public string? AvailabilityStatus { get; set; }

    public decimal? StockQuantity { get; set; }

    public string? Notes { get; set; }

    public bool IsCritical { get; set; }

    public DateTime CreatedDate { get; set; }

    public virtual RawMaterial? Material { get; set; }

    public virtual PurchaseOrder? PurchaseOrder { get; set; }

    public virtual MaterialRequest Request { get; set; } = null!;

    public virtual RawMaterial? SubstituteMaterial { get; set; }

    public virtual Supplier? Supplier { get; set; }

    public virtual Unit? Unit { get; set; }
}
