using System;
using System.Collections.Generic;

namespace Mehrsam_Darou.Models;

public partial class SalaryInfo
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public decimal BaseSalary { get; set; }

    public decimal HourlyRate { get; set; }

    public decimal OvertimeRate { get; set; }

    public string Currency { get; set; } = null!;

    public DateOnly EffectiveDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public string? PersianEffectiveDate { get; set; }

    public string? PersianEndDate { get; set; }

    public DateTime? DateCreated { get; set; }

    public virtual User User { get; set; } = null!;
}
