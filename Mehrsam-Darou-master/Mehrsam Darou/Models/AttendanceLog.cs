using System;
using System.Collections.Generic;

namespace Mehrsam_Darou.Models;

public partial class AttendanceLog
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public DateTime LogTime { get; set; }

    public string LogType { get; set; } = null!;

    public string? DeviceId { get; set; }

    public string? Location { get; set; }

    public string? PersianDate { get; set; }

    public DateTime? DateCreated { get; set; }

    public virtual User User { get; set; } = null!;
}
