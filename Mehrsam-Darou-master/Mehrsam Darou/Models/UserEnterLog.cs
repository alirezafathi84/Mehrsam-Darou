using System;
using System.Collections.Generic;

namespace Mehrsam_Darou.Models;

public partial class UserEnterLog
{
    public Guid Id { get; set; }

    public Guid? UserId { get; set; }

    public DateTime? CreatedDate { get; set; }

    public string? Status { get; set; }

    public virtual User? User { get; set; }
}
