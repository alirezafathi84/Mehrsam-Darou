using System;
using System.Collections.Generic;

namespace Mehrsam_Darou.Models;

public partial class VacationType
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public bool IsPaid { get; set; }

    public int? MaxDaysPerYear { get; set; }

    public virtual ICollection<Vacation> Vacations { get; set; } = new List<Vacation>();
}
