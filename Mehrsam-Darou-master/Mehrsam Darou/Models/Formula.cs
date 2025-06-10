using System;
using System.Collections.Generic;

namespace Mehrsam_Darou.Models;

public partial class Formula
{
    public Guid FormulaId { get; set; }

    public string FormulaCode { get; set; } = null!;

    public string FormulaName { get; set; } = null!;

    public string? FormulaVersion { get; set; }

    public string? FormulaType { get; set; }

    public string? FormulaCategory { get; set; }

    public string? FormulaStatus { get; set; }

    public string? Description { get; set; }

    public Guid? MedicineId { get; set; }

    public string? DosageForm { get; set; }

    public decimal? TargetStrength { get; set; }

    public Guid? StrengthUnitId { get; set; }

    public decimal? BatchSize { get; set; }

    public Guid? BatchSizeUnitId { get; set; }

    public string? ManufacturingMethod { get; set; }

    public string? MixingInstructions { get; set; }

    public string? ProcessingParameters { get; set; }

    public string? QualityControlTests { get; set; }

    public string? StabilityData { get; set; }

    public int? ShelfLifeMonths { get; set; }

    public string? StorageConditions { get; set; }

    public string? PackagingRequirements { get; set; }

    public string? RegulatoryStatus { get; set; }

    public DateOnly? ApprovalDate { get; set; }

    public DateOnly? ExpiryDate { get; set; }

    public string? PharmacologicalClass { get; set; }

    public string? TherapeuticCategory { get; set; }

    public string? Indication { get; set; }

    public string? Contraindications { get; set; }

    public string? SideEffects { get; set; }

    public string? DrugInteractions { get; set; }

    public string? DosageInstructions { get; set; }

    public string? AdministrationRoute { get; set; }

    public decimal? Bioavailability { get; set; }

    public string? BioequivalenceStudy { get; set; }

    public string? FormulationChallenges { get; set; }

    public string? DevelopmentNotes { get; set; }

    public string? ScaleUpConsiderations { get; set; }

    public string? CostAnalysis { get; set; }

    public decimal? UnitCost { get; set; }

    public string? Currency { get; set; }

    public string? CompetitorAnalysis { get; set; }

    public string? MarketPositioning { get; set; }

    public string? IntellectualProperty { get; set; }

    public string? PatentStatus { get; set; }

    public DateOnly? PatentExpiryDate { get; set; }

    public string? LiteratureReferences { get; set; }

    public string? ClinicalData { get; set; }

    public string? PreclinicalData { get; set; }

    public string? AnalyticalMethods { get; set; }

    public string? ValidationStatus { get; set; }

    public DateOnly? ValidationDate { get; set; }

    public string? ValidatedBy { get; set; }

    public string? ChangeControlNumber { get; set; }

    public string? ChangeReason { get; set; }

    public string? ImpactAssessment { get; set; }

    public string? RiskAssessment { get; set; }

    public string? EnvironmentalImpact { get; set; }

    public string? WasteDisposal { get; set; }

    public string? SafetyPrecautions { get; set; }

    public string? HandlingInstructions { get; set; }

    public string? EquipmentRequirements { get; set; }

    public string? FacilityRequirements { get; set; }

    public string? PersonnelQualifications { get; set; }

    public string? TrainingRequirements { get; set; }

    public string? SopReferences { get; set; }

    public string? DocumentLocation { get; set; }

    public string? BatchRecordsLocation { get; set; }

    public string? AnalyticalDataLocation { get; set; }

    public string? StabilityDataLocation { get; set; }

    public bool? IsMasterFormula { get; set; }

    public Guid? ParentFormulaId { get; set; }

    public string? VersionHistory { get; set; }

    public string? ApprovalWorkflow { get; set; }

    public string? ReviewerComments { get; set; }

    public DateOnly? NextReviewDate { get; set; }

    public int? ReviewFrequencyMonths { get; set; }

    public string? ConfidentialityLevel { get; set; }

    public string? AccessRestrictions { get; set; }

    public string? BackupFrequency { get; set; }

    public int? RetentionPeriodYears { get; set; }

    public DateOnly? ArchiveDate { get; set; }

    public string? ArchiveReason { get; set; }

    public string? Tags { get; set; }

    public string? Keywords { get; set; }

    public string? Notes { get; set; }

    public bool? IsActive { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime CreatedDate { get; set; }

    public Guid? LastModifiedBy { get; set; }

    public DateTime? LastModifiedDate { get; set; }

    public virtual Unit? BatchSizeUnit { get; set; }

    public virtual User? CreatedByNavigation { get; set; }

    public virtual ICollection<FormulaIngredient> FormulaIngredients { get; set; } = new List<FormulaIngredient>();

    public virtual ICollection<Formula> InverseParentFormula { get; set; } = new List<Formula>();

    public virtual User? LastModifiedByNavigation { get; set; }

    public virtual Medicine? Medicine { get; set; }

    public virtual Formula? ParentFormula { get; set; }

    public virtual Unit? StrengthUnit { get; set; }
}
