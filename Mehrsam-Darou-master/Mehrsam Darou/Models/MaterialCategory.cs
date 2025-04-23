using System;
using System.Collections.Generic;

namespace Mehrsam_Darou.Models;

public partial class MaterialCategory
{
    public Guid CategoryId { get; set; }

    public string CategoryName { get; set; } = null!;

    public string? Description { get; set; }

    public string? StorageRequirements { get; set; }

    public string? HazardClass { get; set; }

    public bool? IsActive { get; set; }

    public virtual ICollection<RawMaterial> RawMaterials { get; set; } = new List<RawMaterial>();
}
