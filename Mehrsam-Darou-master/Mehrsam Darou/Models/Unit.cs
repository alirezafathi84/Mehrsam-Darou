using System;
using System.Collections.Generic;

namespace Mehrsam_Darou.Models;

public partial class Unit
{
    public Guid UnitId { get; set; }

    public Guid UnitTypeId { get; set; }

    public string UnitName { get; set; } = null!;

    public string UnitSymbol { get; set; } = null!;

    public bool? IsBaseUnit { get; set; }

    public decimal? ConversionFactor { get; set; }

    public string? Description { get; set; }

    public bool? IsActive { get; set; }

    public virtual ICollection<FinishedGoodsBatch> FinishedGoodsBatches { get; set; } = new List<FinishedGoodsBatch>();

    public virtual ICollection<MaterialBatch> MaterialBatches { get; set; } = new List<MaterialBatch>();

    public virtual ICollection<MedicineBom> MedicineBoms { get; set; } = new List<MedicineBom>();

    public virtual ICollection<Medicine> Medicines { get; set; } = new List<Medicine>();

    public virtual ICollection<ProductionOrder> ProductionOrders { get; set; } = new List<ProductionOrder>();

    public virtual ICollection<RawMaterial> RawMaterials { get; set; } = new List<RawMaterial>();

    public virtual ICollection<StorageLocation> StorageLocations { get; set; } = new List<StorageLocation>();

    public virtual UnitType UnitType { get; set; } = null!;
}
