using System;
using System.Collections.Generic;

namespace Mehrsam_Darou.Models;

public partial class QaStandard
{
    public Guid StandardId { get; set; }

    public string StandardCode { get; set; } = null!;

    public string StandardName { get; set; } = null!;

    public string? StandardType { get; set; }

    public string? StandardCategory { get; set; }

    public string? IssuingOrganization { get; set; }

    public string? IssuingCountry { get; set; }

    public string? Description { get; set; }

    public string? Scope { get; set; }

    public string? Version { get; set; }

    public DateOnly? PublicationDate { get; set; }

    public DateOnly? EffectiveDate { get; set; }

    public DateOnly? ReviewDate { get; set; }

    public DateOnly? WithdrawalDate { get; set; }

    public string? Status { get; set; }

    public string? ComplianceLevel { get; set; }

    public string? ApplicableProducts { get; set; }

    public string? ApplicableProcesses { get; set; }

    public string? RelatedRegulations { get; set; }

    public string? ImplementationGuidelines { get; set; }

    public string? AssessmentCriteria { get; set; }

    public string? DocumentationRequirements { get; set; }

    public string? TrainingRequirements { get; set; }

    public int? AuditFrequencyMonths { get; set; }

    public DateOnly? NextReviewDate { get; set; }

    public string? ResponsibleDepartment { get; set; }

    public string? ResponsiblePerson { get; set; }

    public string? ContactEmail { get; set; }

    public string? ContactPhone { get; set; }

    public string? DocumentPath { get; set; }

    public string? ExternalLink { get; set; }

    public decimal? ImplementationCost { get; set; }

    public decimal? MaintenanceCost { get; set; }

    public string? Currency { get; set; }

    public int? PriorityLevel { get; set; }

    public string? RiskLevel { get; set; }

    public string? ComplianceStatus { get; set; }

    public DateOnly? LastAssessmentDate { get; set; }

    public DateOnly? NextAssessmentDate { get; set; }

    public decimal? CompliancePercentage { get; set; }

    public int? NonConformitiesCount { get; set; }

    public int? ActionItemsCount { get; set; }

    public string? Notes { get; set; }

    public string? Tags { get; set; }

    public bool? IsMandatory { get; set; }

    public bool? IsActive { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime CreatedDate { get; set; }

    public Guid? LastModifiedBy { get; set; }

    public DateTime? LastModifiedDate { get; set; }

    public virtual User? CreatedByNavigation { get; set; }

    public virtual User? LastModifiedByNavigation { get; set; }
}
