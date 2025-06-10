using System;
using System.Collections.Generic;

namespace Mehrsam_Darou.Models;

public partial class QaAudit
{
    public Guid AuditId { get; set; }

    public string AuditCode { get; set; } = null!;

    public string AuditTitle { get; set; } = null!;

    public string? AuditType { get; set; }

    public string? AuditCategory { get; set; }

    public string? AuditScope { get; set; }

    public string? AuditedDepartment { get; set; }

    public string? AuditedProcess { get; set; }

    public Guid? RelatedProductId { get; set; }

    public string? RelatedBatchNumber { get; set; }

    public string? AuditStandard { get; set; }

    public DateOnly? PlannedStartDate { get; set; }

    public DateOnly? PlannedEndDate { get; set; }

    public DateOnly? ActualStartDate { get; set; }

    public DateOnly? ActualEndDate { get; set; }

    public decimal? AuditDurationHours { get; set; }

    public string? LeadAuditor { get; set; }

    public string? AuditTeamMembers { get; set; }

    public string? AuditeeDepartmentHead { get; set; }

    public string? AuditeeContactPerson { get; set; }

    public string? AuditStatus { get; set; }

    public int? AuditPriority { get; set; }

    public string? AuditObjectives { get; set; }

    public string? AuditMethodology { get; set; }

    public string? KeyFindings { get; set; }

    public int? ObservationsCount { get; set; }

    public int? MinorNonconformities { get; set; }

    public int? MajorNonconformities { get; set; }

    public int? CriticalNonconformities { get; set; }

    public int? OpportunitiesForImprovement { get; set; }

    public string? OverallRating { get; set; }

    public decimal? CompliancePercentage { get; set; }

    public bool? CorrectiveActionsRequired { get; set; }

    public bool? PreventiveActionsRequired { get; set; }

    public bool? FollowUpRequired { get; set; }

    public DateOnly? FollowUpDate { get; set; }

    public DateOnly? ClosureDate { get; set; }

    public string? AuditReportPath { get; set; }

    public string? EvidenceDocumentsPath { get; set; }

    public decimal? Cost { get; set; }

    public string? Currency { get; set; }

    public string? ExternalAuditorCompany { get; set; }

    public string? RegulatoryImpact { get; set; }

    public string? RiskLevel { get; set; }

    public bool? ManagementReviewRequired { get; set; }

    public DateOnly? ManagementReviewDate { get; set; }

    public string? LessonsLearned { get; set; }

    public string? Recommendations { get; set; }

    public string? Notes { get; set; }

    public bool? IsActive { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime CreatedDate { get; set; }

    public Guid? LastModifiedBy { get; set; }

    public DateTime? LastModifiedDate { get; set; }

    public virtual User? CreatedByNavigation { get; set; }

    public virtual User? LastModifiedByNavigation { get; set; }

    public virtual Medicine? RelatedProduct { get; set; }
}
