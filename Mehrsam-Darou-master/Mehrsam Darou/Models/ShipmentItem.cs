using System;
using System.Collections.Generic;

namespace Mehrsam_Darou.Models;

public partial class ShipmentItem
{
    public Guid ShipmentItemId { get; set; }

    public Guid ShipmentId { get; set; }

    public Guid MedicineId { get; set; }

    public Guid? BatchId { get; set; }

    public decimal Quantity { get; set; }

    public Guid UnitId { get; set; }

    public string? Notes { get; set; }

    public virtual FinishedGoodsBatch? Batch { get; set; }

    public virtual Medicine Medicine { get; set; } = null!;

    public virtual Shipment Shipment { get; set; } = null!;

    public virtual Unit Unit { get; set; } = null!;
}
