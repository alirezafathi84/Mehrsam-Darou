using System;
using System.Collections.Generic;

namespace Mehrsam_Darou.Models;

public partial class FinancialReport
{
    public Guid ReportId { get; set; }

    public string ReportCode { get; set; } = null!;

    public string ReportName { get; set; } = null!;

    public string ReportType { get; set; } = null!;

    public string PeriodType { get; set; } = null!;

    public DateOnly PeriodStart { get; set; }

    public DateOnly PeriodEnd { get; set; }

    public int FiscalYear { get; set; }

    public string? Status { get; set; }

    public DateTime? GeneratedDate { get; set; }

    public string? ReportData { get; set; }

    public string? FilePath { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime CreatedDate { get; set; }

    public Guid? ApprovedBy { get; set; }

    public DateTime? ApprovedDate { get; set; }

    public virtual User? ApprovedByNavigation { get; set; }

    public virtual User? CreatedByNavigation { get; set; }
}
