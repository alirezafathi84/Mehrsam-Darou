using System;
using System.Collections.Generic;

namespace Mehrsam_Darou.Models;

public partial class UnitType
{
    public Guid UnitTypeId { get; set; }

    public string TypeName { get; set; } = null!;

    public string? Description { get; set; }

    public bool? IsSystem { get; set; }

    public virtual ICollection<Unit> Units { get; set; } = new List<Unit>();
}
