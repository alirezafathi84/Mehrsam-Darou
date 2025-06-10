using System;
using System.Collections.Generic;

namespace Mehrsam_Darou.Models;

public partial class ClinicalTrial
{
    public Guid TrialId { get; set; }

    public string TrialCode { get; set; } = null!;

    public string TrialName { get; set; } = null!;

    public Guid? ProjectId { get; set; }

    public Guid? FormulationId { get; set; }

    public string? TrialPhase { get; set; }

    public string? TrialType { get; set; }

    public string? StudyDesign { get; set; }

    public string? PrimaryEndpoint { get; set; }

    public string? SecondaryEndpoints { get; set; }

    public string? InclusionCriteria { get; set; }

    public string? ExclusionCriteria { get; set; }

    public int? TargetEnrollment { get; set; }

    public int? ActualEnrollment { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? ExpectedCompletionDate { get; set; }

    public DateOnly? ActualCompletionDate { get; set; }

    public string? PrincipalInvestigator { get; set; }

    public string? Sponsor { get; set; }

    public decimal? Budget { get; set; }

    public string? Status { get; set; }

    public string? RegulatoryApproval { get; set; }

    public string? EthicsApproval { get; set; }

    public string? ResultsSummary { get; set; }

    public string? AdverseEvents { get; set; }

    public string? Conclusions { get; set; }

    public string? PublicationReferences { get; set; }

    public string? Notes { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime CreatedDate { get; set; }

    public bool? IsActive { get; set; }

    public virtual Formulation? Formulation { get; set; }

    public virtual ResearchProject? Project { get; set; }
}
