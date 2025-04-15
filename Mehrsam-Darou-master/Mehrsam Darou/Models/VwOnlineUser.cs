using System;
using System.Collections.Generic;

namespace Mehrsam_Darou.Models;

public partial class VwOnlineUser
{
    public Guid Id { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? Username { get; set; }

    public string? AvatarImg { get; set; }

    public string Status { get; set; } = null!;

    public DateTime LastSeen { get; set; }
}
