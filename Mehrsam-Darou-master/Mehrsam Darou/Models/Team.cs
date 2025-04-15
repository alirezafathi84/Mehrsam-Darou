using System;
using System.Collections.Generic;

namespace Mehrsam_Darou.Models;

public partial class Team
{
    public Guid Id { get; set; }

    public string? Name { get; set; }

    public string? DefaultPageForTeam { get; set; }

    public bool? IsActive { get; set; }

    public bool? ManagmentDashboard { get; set; }

    public bool? Setting { get; set; }

    public bool? SystemUsers { get; set; }

    public bool? Financial { get; set; }

    public bool? Inventory { get; set; }

    public bool? Product { get; set; }

    public bool? SellCommercial { get; set; }

    public bool? BuyCommercial { get; set; }

    public bool? RandD { get; set; }

    public bool? Qc { get; set; }

    public bool? Qa { get; set; }

    public bool? Pmo { get; set; }

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
