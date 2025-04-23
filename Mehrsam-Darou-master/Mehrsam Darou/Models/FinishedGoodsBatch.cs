using System;
using System.Collections.Generic;

namespace Mehrsam_Darou.Models;

public partial class FinishedGoodsBatch
{
    public Guid BatchId { get; set; }

    public Guid MedicineId { get; set; }

    public string BatchNumber { get; set; } = null!;

    public Guid? OrderId { get; set; }

    public decimal Quantity { get; set; }

    public Guid UnitId { get; set; }

    public Guid? LocationId { get; set; }

    public DateOnly ManufactureDate { get; set; }

    public DateOnly ExpiryDate { get; set; }

    public string? Status { get; set; }

    public virtual StorageLocation? Location { get; set; }

    public virtual Medicine Medicine { get; set; } = null!;

    public virtual ProductionOrder? Order { get; set; }

    public virtual Unit Unit { get; set; } = null!;
}
