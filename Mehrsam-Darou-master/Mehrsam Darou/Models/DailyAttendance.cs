using System;
using System.Collections.Generic;

namespace Mehrsam_Darou.Models;

public partial class DailyAttendance
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public DateOnly Date { get; set; }

    public string? PersianDate { get; set; }

    public TimeOnly? FirstEntryTime { get; set; }

    public TimeOnly? LastExitTime { get; set; }

    public decimal? TotalHours { get; set; }

    public string Status { get; set; } = null!;

    public bool IsWorkingDay { get; set; }

    public DateTime? DateCreated { get; set; }

    public virtual User User { get; set; } = null!;
}
