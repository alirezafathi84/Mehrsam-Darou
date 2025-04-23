using System;
using System.Collections.Generic;

namespace Mehrsam_Darou.Models;

public partial class RawMaterial
{
    public Guid MaterialId { get; set; }

    public string MaterialCode { get; set; } = null!;

    public string MaterialName { get; set; } = null!;

    public Guid CategoryId { get; set; }

    public Guid UnitId { get; set; }

    public decimal? MinStockLevel { get; set; }

    public decimal? MaxStockLevel { get; set; }

    public int? LeadTimeDays { get; set; }

    public bool? IsActive { get; set; }

    public virtual MaterialCategory Category { get; set; } = null!;

    public virtual ICollection<MaterialBatch> MaterialBatches { get; set; } = new List<MaterialBatch>();

    public virtual ICollection<MedicineBom> MedicineBoms { get; set; } = new List<MedicineBom>();

    public virtual Unit Unit { get; set; } = null!;
}
