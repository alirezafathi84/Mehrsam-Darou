using System;
using System.Collections.Generic;

namespace Mehrsam_Darou.Models;

public partial class QcTest
{
    public Guid TestId { get; set; }

    public string TestCode { get; set; } = null!;

    public string TestName { get; set; } = null!;

    public string? TestType { get; set; }

    public string? TestCategory { get; set; }

    public string? TestMethod { get; set; }

    public string? StandardReference { get; set; }

    public string? Description { get; set; }

    public string? TestProcedure { get; set; }

    public string? EquipmentRequired { get; set; }

    public string? ReagentsRequired { get; set; }

    public string? SamplePreparation { get; set; }

    public string? AcceptanceCriteria { get; set; }

    public decimal? SpecificationMin { get; set; }

    public decimal? SpecificationMax { get; set; }

    public string? UnitOfMeasure { get; set; }

    public int? TestDurationMinutes { get; set; }

    public string? TemperatureCondition { get; set; }

    public string? HumidityCondition { get; set; }

    public string? StorageCondition { get; set; }

    public string? Frequency { get; set; }

    public bool? CalibrationRequired { get; set; }

    public bool? EnvironmentalControl { get; set; }

    public string? SafetyRequirements { get; set; }

    public string? OperatorQualification { get; set; }

    public string? DataIntegrityLevel { get; set; }

    public bool? ApprovalRequired { get; set; }

    public decimal? CostPerTest { get; set; }

    public string? Currency { get; set; }

    public string? ApplicableProducts { get; set; }

    public string? ApplicableStages { get; set; }

    public string? RelatedTests { get; set; }

    public bool? TrendAnalysisRequired { get; set; }

    public bool? StatisticalControl { get; set; }

    public decimal? DeviationThreshold { get; set; }

    public decimal? AlertLimit { get; set; }

    public decimal? ActionLimit { get; set; }

    public string? RetestCriteria { get; set; }

    public bool? StabilityImpact { get; set; }

    public bool? RegulatoryRequirement { get; set; }

    public string? ValidationStatus { get; set; }

    public DateOnly? LastValidationDate { get; set; }

    public DateOnly? NextValidationDate { get; set; }

    public bool? ChangeControlRequired { get; set; }

    public string? ResponsibleDepartment { get; set; }

    public string? ResponsiblePerson { get; set; }

    public string? BackupPerson { get; set; }

    public string? TrainingRequired { get; set; }

    public string? DocumentReferences { get; set; }

    public string? RevisionHistory { get; set; }

    public DateOnly? EffectiveDate { get; set; }

    public DateOnly? ReviewDate { get; set; }

    public DateOnly? RetirementDate { get; set; }

    public int? PriorityLevel { get; set; }

    public string? RiskLevel { get; set; }

    public string? ImpactLevel { get; set; }

    public string? Notes { get; set; }

    public string? Tags { get; set; }

    public bool? IsActive { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime CreatedDate { get; set; }

    public Guid? LastModifiedBy { get; set; }

    public DateTime? LastModifiedDate { get; set; }

    public virtual ICollection<BatchTest> BatchTests { get; set; } = new List<BatchTest>();

    public virtual User? CreatedByNavigation { get; set; }

    public virtual User? LastModifiedByNavigation { get; set; }
}
