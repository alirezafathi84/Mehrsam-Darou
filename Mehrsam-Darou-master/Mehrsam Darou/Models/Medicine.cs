using System;
using System.Collections.Generic;

namespace Mehrsam_Darou.Models;

public partial class Medicine
{
    public Guid MedicineId { get; set; }

    public string MedicineCode { get; set; } = null!;

    public string BrandName { get; set; } = null!;

    public Guid CategoryId { get; set; }

    public decimal? Strength { get; set; }

    public Guid? StrengthUnitId { get; set; }

    public int? ShelfLifeMonths { get; set; }

    public bool? IsActive { get; set; }

    public virtual MedicineCategory Category { get; set; } = null!;

    public virtual ICollection<FinishedGoodsBatch> FinishedGoodsBatches { get; set; } = new List<FinishedGoodsBatch>();

    public virtual ICollection<MedicineBom> MedicineBoms { get; set; } = new List<MedicineBom>();

    public virtual ICollection<ProductionOrder> ProductionOrders { get; set; } = new List<ProductionOrder>();

    public virtual Unit? StrengthUnit { get; set; }
}
