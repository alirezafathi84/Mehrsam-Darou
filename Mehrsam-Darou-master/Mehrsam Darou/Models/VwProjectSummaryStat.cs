using System;
using System.Collections.Generic;

namespace Mehrsam_Darou.Models;

public partial class VwProjectSummaryStat
{
    public string? ProjectStatus { get; set; }

    public string? ProjectType { get; set; }

    public string? ProjectCategory { get; set; }

    public int? ProjectCount { get; set; }

    public decimal? AvgCompletion { get; set; }

    public decimal? TotalBudgetSum { get; set; }

    public decimal? TotalSpentSum { get; set; }

    public int? AvgPriority { get; set; }
}
