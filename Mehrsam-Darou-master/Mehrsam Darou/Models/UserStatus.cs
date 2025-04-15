using System;
using System.Collections.Generic;

namespace Mehrsam_Darou.Models;

public partial class UserStatus
{
    public Guid UserId { get; set; }

    public string Status { get; set; } = null!;

    public DateTime LastSeen { get; set; }

    public virtual User User { get; set; } = null!;
}
