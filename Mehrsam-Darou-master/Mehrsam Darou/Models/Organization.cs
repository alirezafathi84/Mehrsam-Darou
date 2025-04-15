using System;
using System.Collections.Generic;

namespace Mehrsam_Darou.Models;

public partial class Organization
{
    public Guid Id { get; set; }

    public string? Name { get; set; }

    public int Priority { get; set; }
}
