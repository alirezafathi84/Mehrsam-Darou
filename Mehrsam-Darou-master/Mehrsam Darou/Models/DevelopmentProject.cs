using System;
using System.Collections.Generic;

namespace Mehrsam_Darou.Models;

public partial class DevelopmentProject
{
    public Guid ProjectId { get; set; }

    public string ProjectCode { get; set; } = null!;

    public string ProjectName { get; set; } = null!;

    public string? ProjectType { get; set; }

    public string? ProjectCategory { get; set; }

    public string? ProjectStatus { get; set; }

    public int? PriorityLevel { get; set; }

    public string? Description { get; set; }

    public string? Objectives { get; set; }

    public string? Scope { get; set; }

    public Guid? TargetMedicineId { get; set; }

    public string? DevelopmentStage { get; set; }

    public DateOnly? PlannedStartDate { get; set; }

    public DateOnly? PlannedEndDate { get; set; }

    public DateOnly? ActualStartDate { get; set; }

    public DateOnly? ActualEndDate { get; set; }

    public int? EstimatedDurationMonths { get; set; }

    public int? ActualDurationMonths { get; set; }

    public string? ProjectManager { get; set; }

    public string? TeamLead { get; set; }

    public string? TeamMembers { get; set; }

    public decimal? BudgetAllocated { get; set; }

    public decimal? BudgetSpent { get; set; }

    public decimal? BudgetRemaining { get; set; }

    public string? Currency { get; set; }

    public string? FundingSource { get; set; }

    public string? ResearchObjectives { get; set; }

    public string? Methodology { get; set; }

    public string? KeyMilestones { get; set; }

    public string? Deliverables { get; set; }

    public string? SuccessCriteria { get; set; }

    public string? RiskAssessment { get; set; }

    public string? MitigationStrategies { get; set; }

    public string? RegulatoryRequirements { get; set; }

    public string? ComplianceStatus { get; set; }

    public string? IpConsiderations { get; set; }

    public string? PatentStatus { get; set; }

    public string? LiteratureReview { get; set; }

    public string? CompetitiveAnalysis { get; set; }

    public string? MarketPotential { get; set; }

    public string? TargetMarket { get; set; }

    public string? FormulationDetails { get; set; }

    public string? ManufacturingConsiderations { get; set; }

    public string? QualityRequirements { get; set; }

    public string? TestingRequirements { get; set; }

    public string? ClinicalConsiderations { get; set; }

    public string? RegulatoryPathway { get; set; }

    public string? ApprovalTimeline { get; set; }

    public string? CollaborationPartners { get; set; }

    public string? ExternalConsultants { get; set; }

    public string? EquipmentRequirements { get; set; }

    public string? FacilityRequirements { get; set; }

    public string? TechnologyTransfer { get; set; }

    public string? ScaleUpConsiderations { get; set; }

    public string? CommercializationPlan { get; set; }

    public string? LaunchTimeline { get; set; }

    public decimal? ProgressPercentage { get; set; }

    public string? CurrentPhase { get; set; }

    public string? NextMilestone { get; set; }

    public DateOnly? NextMilestoneDate { get; set; }

    public string? KeyAchievements { get; set; }

    public string? ChallengesFaced { get; set; }

    public string? LessonsLearned { get; set; }

    public string? Recommendations { get; set; }

    public string? ProjectDocumentsPath { get; set; }

    public string? ResearchDataPath { get; set; }

    public string? ReportsPath { get; set; }

    public string? PresentationsPath { get; set; }

    public bool? QualityReviewRequired { get; set; }

    public bool? RegulatoryReviewRequired { get; set; }

    public bool? ManagementReviewRequired { get; set; }

    public DateOnly? LastReviewDate { get; set; }

    public DateOnly? NextReviewDate { get; set; }

    public string? ApprovalStatus { get; set; }

    public string? ApprovedBy { get; set; }

    public DateOnly? ApprovalDate { get; set; }

    public string? ConfidentialityLevel { get; set; }

    public string? DataClassification { get; set; }

    public string? AccessRestrictions { get; set; }

    public string? BackupFrequency { get; set; }

    public int? RetentionPeriodYears { get; set; }

    public string? Tags { get; set; }

    public string? Keywords { get; set; }

    public string? RelatedProjects { get; set; }

    public string? Dependencies { get; set; }

    public string? ImpactAssessment { get; set; }

    public string? SustainabilityConsiderations { get; set; }

    public string? EnvironmentalImpact { get; set; }

    public string? SocialImpact { get; set; }

    public string? Notes { get; set; }

    public bool? IsActive { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime CreatedDate { get; set; }

    public Guid? LastModifiedBy { get; set; }

    public DateTime? LastModifiedDate { get; set; }

    public virtual User? CreatedByNavigation { get; set; }

    public virtual User? LastModifiedByNavigation { get; set; }

    public virtual Medicine? TargetMedicine { get; set; }
}
