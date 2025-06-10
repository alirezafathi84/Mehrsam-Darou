using System;
using System.Collections.Generic;

namespace Mehrsam_Darou.Models;

public partial class VwActiveResearchProject
{
    public Guid ProjectId { get; set; }

    public string ProjectCode { get; set; } = null!;

    public string ProjectTitle { get; set; } = null!;

    public string? ProjectType { get; set; }

    public string? ProjectCategory { get; set; }

    public string? ProjectStatus { get; set; }

    public int? PriorityLevel { get; set; }

    public decimal? CompletionPercentage { get; set; }

    public string? PrincipalInvestigator { get; set; }

    public string? ProjectManager { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? PlannedEndDate { get; set; }

    public decimal? TotalBudget { get; set; }

    public decimal? SpentBudget { get; set; }

    public string? Currency { get; set; }

    public DateTime CreatedDate { get; set; }

    public decimal? BudgetUtilizationPercentage { get; set; }

    public decimal? TimelineProgressPercentage { get; set; }
}
