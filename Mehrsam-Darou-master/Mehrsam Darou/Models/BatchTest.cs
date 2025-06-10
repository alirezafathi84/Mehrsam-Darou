using System;
using System.Collections.Generic;

namespace Mehrsam_Darou.Models;

public partial class BatchTest
{
    public Guid BatchTestId { get; set; }

    public string TestNumber { get; set; } = null!;

    public Guid BatchId { get; set; }

    public string BatchNumber { get; set; } = null!;

    public Guid ProductId { get; set; }

    public Guid TestId { get; set; }

    public string? TestStage { get; set; }

    public string? SampleId { get; set; }

    public string? SampleSource { get; set; }

    public string? SampleLocation { get; set; }

    public decimal? SampleQuantity { get; set; }

    public string? SampleUnit { get; set; }

    public DateTime? SamplingDate { get; set; }

    public string? SamplingPerson { get; set; }

    public string? SampleCondition { get; set; }

    public DateTime? TestStartDate { get; set; }

    public DateTime? TestCompletionDate { get; set; }

    public int? TestDurationActual { get; set; }

    public string? TestedBy { get; set; }

    public string? ReviewedBy { get; set; }

    public string? ApprovedBy { get; set; }

    public string? TestStatus { get; set; }

    public string? TestResult { get; set; }

    public decimal? TestValue { get; set; }

    public string? TestUnit { get; set; }

    public decimal? SpecificationMin { get; set; }

    public decimal? SpecificationMax { get; set; }

    public bool? AcceptanceCriteriaMet { get; set; }

    public string? PassFailStatus { get; set; }

    public decimal? DeviationPercentage { get; set; }

    public bool? OutlierInvestigation { get; set; }

    public bool? RetestRequired { get; set; }

    public string? RetestReason { get; set; }

    public int? RetestCount { get; set; }

    public Guid? OriginalTestId { get; set; }

    public string? InstrumentUsed { get; set; }

    public string? InstrumentId { get; set; }

    public string? CalibrationStatus { get; set; }

    public DateOnly? CalibrationDate { get; set; }

    public string? MethodReference { get; set; }

    public string? EnvironmentalConditions { get; set; }

    public decimal? TemperatureRecorded { get; set; }

    public decimal? HumidityRecorded { get; set; }

    public string? AnalystQualification { get; set; }

    public bool? SupervisorReview { get; set; }

    public bool? QaReview { get; set; }

    public bool? DataIntegrityCheck { get; set; }

    public bool? ElectronicSignature { get; set; }

    public string? RawDataLocation { get; set; }

    public string? ChromatogramPath { get; set; }

    public string? SpectrumPath { get; set; }

    public string? ImagePath { get; set; }

    public string? CalculationFormula { get; set; }

    public string? CalculationPerformedBy { get; set; }

    public string? CalculationCheckedBy { get; set; }

    public string? StatisticalAnalysis { get; set; }

    public string? TrendAnalysis { get; set; }

    public string? HistoricalComparison { get; set; }

    public bool? InvestigationRequired { get; set; }

    public string? InvestigationNumber { get; set; }

    public string? DeviationNumber { get; set; }

    public bool? CapaRequired { get; set; }

    public string? CapaNumber { get; set; }

    public string? RegulatoryImpact { get; set; }

    public bool? CustomerNotification { get; set; }

    public string? LotDisposition { get; set; }

    public string? ReleaseDecision { get; set; }

    public DateTime? ReleaseDate { get; set; }

    public string? ReleasedBy { get; set; }

    public string? HoldReason { get; set; }

    public string? RejectReason { get; set; }

    public string? Comments { get; set; }

    public int? TestPriority { get; set; }

    public bool? RushTest { get; set; }

    public decimal? Cost { get; set; }

    public string? Currency { get; set; }

    public bool? ExternalLab { get; set; }

    public string? LabName { get; set; }

    public string? CertificateNumber { get; set; }

    public string? CertificatePath { get; set; }

    public bool? StabilityStudy { get; set; }

    public string? StabilityCondition { get; set; }

    public int? StorageTime { get; set; }

    public string? StorageUnit { get; set; }

    public bool? ExpiryImpact { get; set; }

    public bool? ShelfLifeStudy { get; set; }

    public string? Notes { get; set; }

    public bool? IsActive { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime CreatedDate { get; set; }

    public Guid? LastModifiedBy { get; set; }

    public DateTime? LastModifiedDate { get; set; }

    public virtual User? CreatedByNavigation { get; set; }

    public virtual ICollection<BatchTest> InverseOriginalTest { get; set; } = new List<BatchTest>();

    public virtual User? LastModifiedByNavigation { get; set; }

    public virtual BatchTest? OriginalTest { get; set; }

    public virtual Medicine Product { get; set; } = null!;

    public virtual QcTest Test { get; set; } = null!;
}
