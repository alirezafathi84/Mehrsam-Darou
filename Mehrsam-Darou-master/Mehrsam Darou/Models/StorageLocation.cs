using System;
using System.Collections.Generic;

namespace Mehrsam_Darou.Models;

public partial class StorageLocation
{
    public Guid LocationId { get; set; }

    public string LocationCode { get; set; } = null!;

    public string LocationName { get; set; } = null!;

    public string? LocationType { get; set; }

    public string? TemperatureRange { get; set; }

    public decimal? Capacity { get; set; }

    public Guid? CapacityUnitId { get; set; }

    public bool? IsActive { get; set; }

    public virtual Unit? CapacityUnit { get; set; }

    public virtual ICollection<FinishedGoodsBatch> FinishedGoodsBatches { get; set; } = new List<FinishedGoodsBatch>();

    public virtual ICollection<MaterialBatch> MaterialBatches { get; set; } = new List<MaterialBatch>();
}
