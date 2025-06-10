using System;
using System.Collections.Generic;

namespace Mehrsam_Darou.Models;

public partial class RegulatorySubmission
{
    public Guid SubmissionId { get; set; }

    public string SubmissionCode { get; set; } = null!;

    public string? SubmissionType { get; set; }

    public Guid? ProjectId { get; set; }

    public Guid? FormulationId { get; set; }

    public string? RegulatoryAuthority { get; set; }

    public DateOnly? SubmissionDate { get; set; }

    public string? ReferenceNumber { get; set; }

    public string? ProductName { get; set; }

    public string? Indication { get; set; }

    public string? DosageForm { get; set; }

    public string? Strength { get; set; }

    public string? SubmissionStatus { get; set; }

    public int? ReviewTimeline { get; set; }

    public DateOnly? TargetApprovalDate { get; set; }

    public DateOnly? ActualApprovalDate { get; set; }

    public string? ApprovalNumber { get; set; }

    public string? ConditionsOfApproval { get; set; }

    public decimal? AnnualFee { get; set; }

    public DateOnly? RenewalDate { get; set; }

    public Guid? ResponsiblePerson { get; set; }

    public string? ConsultantInfo { get; set; }

    public string? DocumentationPath { get; set; }

    public string? CorrespondenceLog { get; set; }

    public string? DeficiencyLetters { get; set; }

    public string? ResponsesSubmitted { get; set; }

    public string? Notes { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime CreatedDate { get; set; }

    public bool? IsActive { get; set; }

    public virtual Formulation? Formulation { get; set; }

    public virtual ResearchProject? Project { get; set; }
}
