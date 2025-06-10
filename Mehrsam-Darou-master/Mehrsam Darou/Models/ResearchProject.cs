using System;
using System.Collections.Generic;

namespace Mehrsam_Darou.Models;

public partial class ResearchProject
{
    public Guid ProjectId { get; set; }

    public string ProjectCode { get; set; } = null!;

    public string ProjectTitle { get; set; } = null!;

    public string? ProjectType { get; set; }

    public string? ProjectCategory { get; set; }

    public string? ProjectDescription { get; set; }

    public string? Objectives { get; set; }

    public string? ExpectedOutcomes { get; set; }

    public string? Methodology { get; set; }

    public string? RiskAssessment { get; set; }

    public string? ProjectStatus { get; set; }

    public int? PriorityLevel { get; set; }

    public string? CurrentPhase { get; set; }

    public decimal? CompletionPercentage { get; set; }

    public string? PrincipalInvestigator { get; set; }

    public string? ProjectManager { get; set; }

    public string? TeamMembers { get; set; }

    public string? Collaborators { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? PlannedEndDate { get; set; }

    public DateOnly? ActualEndDate { get; set; }

    public decimal? TotalBudget { get; set; }

    public decimal? SpentBudget { get; set; }

    public string? Currency { get; set; }

    public string? ResourcesRequired { get; set; }

    public string? KeyFindings { get; set; }

    public string? Publications { get; set; }

    public string? Patents { get; set; }

    public string? Awards { get; set; }

    public string? ChallengesFaced { get; set; }

    public string? LessonsLearned { get; set; }

    public string? NextSteps { get; set; }

    public DateOnly? LastReviewDate { get; set; }

    public DateOnly? NextReviewDate { get; set; }

    public string? ReviewedBy { get; set; }

    public string? ApprovedBy { get; set; }

    public string? DocumentPath { get; set; }

    public string? Tags { get; set; }

    public string? Notes { get; set; }

    public bool? IsActive { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime CreatedDate { get; set; }

    public Guid? LastModifiedBy { get; set; }

    public DateTime? LastModifiedDate { get; set; }

    public virtual User? CreatedByNavigation { get; set; }

    public virtual User? LastModifiedByNavigation { get; set; }
}
