using System;
using System.Collections.Generic;

namespace Mehrsam_Darou.Models;

public partial class MedicineBom
{
    public Guid BomId { get; set; }

    public Guid MedicineId { get; set; }

    public Guid MaterialId { get; set; }

    public decimal Quantity { get; set; }

    public Guid UnitId { get; set; }

    public bool? IsActive { get; set; }

    public virtual RawMaterial Material { get; set; } = null!;

    public virtual Medicine Medicine { get; set; } = null!;

    public virtual Unit Unit { get; set; } = null!;
}
