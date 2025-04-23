using System;
using System.Collections.Generic;

namespace Mehrsam_Darou.Models;

public partial class ProductionOrder
{
    public Guid OrderId { get; set; }

    public string OrderNumber { get; set; } = null!;

    public Guid MedicineId { get; set; }

    public decimal Quantity { get; set; }

    public Guid UnitId { get; set; }

    public DateOnly TargetDate { get; set; }

    public string? Status { get; set; }

    public int? Priority { get; set; }

    public virtual ICollection<FinishedGoodsBatch> FinishedGoodsBatches { get; set; } = new List<FinishedGoodsBatch>();

    public virtual Medicine Medicine { get; set; } = null!;

    public virtual ICollection<ProductionOrderStep> ProductionOrderSteps { get; set; } = new List<ProductionOrderStep>();

    public virtual Unit Unit { get; set; } = null!;
}
