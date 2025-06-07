using System;
using System.Collections.Generic;

namespace Mehrsam_Darou.Models;

public partial class Vacation
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid TypeId { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public string? PersianStartDate { get; set; }

    public string? PersianEndDate { get; set; }

    public string Status { get; set; } = null!;

    public Guid? ApprovedBy { get; set; }

    public DateTime? ApprovalDate { get; set; }

    public string? Notes { get; set; }

    public DateTime? DateCreated { get; set; }

    public virtual User? ApprovedByNavigation { get; set; }

    public virtual VacationType Type { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
