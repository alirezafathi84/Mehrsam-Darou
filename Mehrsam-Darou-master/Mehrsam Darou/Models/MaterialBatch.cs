using System;
using System.Collections.Generic;

namespace Mehrsam_Darou.Models;

public partial class MaterialBatch
{
    public Guid BatchId { get; set; }

    public Guid MaterialId { get; set; }

    public string BatchNumber { get; set; } = null!;

    public decimal InitialQuantity { get; set; }

    public decimal CurrentQuantity { get; set; }

    public Guid UnitId { get; set; }

    public Guid? LocationId { get; set; }

    public string? Status { get; set; }

    public DateOnly? ExpiryDate { get; set; }

    public virtual StorageLocation? Location { get; set; }

    public virtual RawMaterial Material { get; set; } = null!;

    public virtual Unit Unit { get; set; } = null!;
}
