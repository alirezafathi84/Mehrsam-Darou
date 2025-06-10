using System;
using System.Collections.Generic;

namespace Mehrsam_Darou.Models;

public partial class QcReport
{
    public Guid ReportId { get; set; }

    public string ReportNumber { get; set; } = null!;

    public string ReportTitle { get; set; } = null!;

    public string? ReportType { get; set; }

    public string? ReportCategory { get; set; }

    public string? ReportPeriod { get; set; }

    public DateOnly? PeriodStartDate { get; set; }

    public DateOnly? PeriodEndDate { get; set; }

    public string? ReportScope { get; set; }

    public string? ExecutiveSummary { get; set; }

    public string? Methodology { get; set; }

    public string? DataSources { get; set; }

    public string? KeyFindings { get; set; }

    public string? Conclusions { get; set; }

    public string? Recommendations { get; set; }

    public string? ActionItems { get; set; }

    public string? TrendAnalysis { get; set; }

    public string? StatisticalSummary { get; set; }

    public string? ComplianceStatus { get; set; }

    public string? DeviationSummary { get; set; }

    public string? InvestigationSummary { get; set; }

    public string? CapaSummary { get; set; }

    public string? QualityMetrics { get; set; }

    public string? PerformanceIndicators { get; set; }

    public string? BatchReviewSummary { get; set; }

    public string? ProductPerformance { get; set; }

    public string? EquipmentPerformance { get; set; }

    public string? AnalystPerformance { get; set; }

    public string? MethodPerformance { get; set; }

    public string? StabilitySummary { get; set; }

    public string? EnvironmentalMonitoring { get; set; }

    public string? CalibrationSummary { get; set; }

    public string? TrainingSummary { get; set; }

    public string? DocumentControl { get; set; }

    public string? ChangeControlSummary { get; set; }

    public string? SupplierPerformance { get; set; }

    public string? CustomerComplaints { get; set; }

    public string? RegulatoryUpdates { get; set; }

    public string? AuditFindings { get; set; }

    public string? ImprovementInitiatives { get; set; }

    public string? CostAnalysis { get; set; }

    public string? ResourceUtilization { get; set; }

    public string? CapacityAnalysis { get; set; }

    public string? RiskAssessment { get; set; }

    public string? MitigationStrategies { get; set; }

    public string? ForecastOutlook { get; set; }

    public string? NextPeriodGoals { get; set; }

    public string? PreparedBy { get; set; }

    public string? ReviewedBy { get; set; }

    public string? ApprovedBy { get; set; }

    public DateOnly? PreparationDate { get; set; }

    public DateOnly? ReviewDate { get; set; }

    public DateOnly? ApprovalDate { get; set; }

    public string? DistributionList { get; set; }

    public string? ConfidentialityLevel { get; set; }

    public string? ReportStatus { get; set; }

    public string? Version { get; set; }

    public string? RevisionHistory { get; set; }

    public string? Attachments { get; set; }

    public bool? ChartsIncluded { get; set; }

    public bool? TablesIncluded { get; set; }

    public bool? GraphsIncluded { get; set; }

    public string? DashboardLink { get; set; }

    public string? DataFilePath { get; set; }

    public string? ReportFilePath { get; set; }

    public string? PresentationPath { get; set; }

    public string? RelatedReports { get; set; }

    public bool? FollowUpRequired { get; set; }

    public DateOnly? FollowUpDate { get; set; }

    public string? FollowUpResponsible { get; set; }

    public bool? ManagementReview { get; set; }

    public string? ManagementComments { get; set; }

    public bool? ActionPlanRequired { get; set; }

    public DateOnly? ActionPlanDate { get; set; }

    public bool? EscalationRequired { get; set; }

    public string? EscalationLevel { get; set; }

    public bool? RegulatorySubmission { get; set; }

    public DateOnly? SubmissionDate { get; set; }

    public string? SubmissionAuthority { get; set; }

    public int? PriorityLevel { get; set; }

    public string? ImpactLevel { get; set; }

    public string? Audience { get; set; }

    public string? Keywords { get; set; }

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
