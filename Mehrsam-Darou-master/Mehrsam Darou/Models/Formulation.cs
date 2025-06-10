using System;
using System.Collections.Generic;

namespace Mehrsam_Darou.Models;

public partial class Formulation
{
    public Guid FormulationId { get; set; }

    public string FormulationCode { get; set; } = null!;

    public string FormulationName { get; set; } = null!;

    public Guid? ProjectId { get; set; }

    public string Version { get; set; } = null!;

    public Guid? MedicineId { get; set; }

    public string? DosageForm { get; set; }

    public decimal? Strength { get; set; }

    public Guid? StrengthUnitId { get; set; }

    public decimal? BatchSize { get; set; }

    public Guid? BatchUnitId { get; set; }

    public int? ShelfLifeMonths { get; set; }

    public string? StorageConditions { get; set; }

    public string? ManufacturingProcess { get; set; }

    public string? QualitySpecifications { get; set; }

    public string? StabilityData { get; set; }

    public decimal? CostPerUnit { get; set; }

    public string? DevelopmentStage { get; set; }

    public string? ApprovalStatus { get; set; }

    public Guid? ApprovedBy { get; set; }

    public DateTime? ApprovalDate { get; set; }

    public string? Notes { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime CreatedDate { get; set; }

    public Guid? ModifiedBy { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public bool? IsActive { get; set; }

    public virtual Unit? BatchUnit { get; set; }

    public virtual ICollection<ClinicalTrial> ClinicalTrials { get; set; } = new List<ClinicalTrial>();

    public virtual ICollection<DevelopmentBatch> DevelopmentBatches { get; set; } = new List<DevelopmentBatch>();

    public virtual ICollection<FormulationIngredient> FormulationIngredients { get; set; } = new List<FormulationIngredient>();

    public virtual ICollection<IntellectualProperty> IntellectualProperties { get; set; } = new List<IntellectualProperty>();

    public virtual ICollection<LaboratoryTest> LaboratoryTests { get; set; } = new List<LaboratoryTest>();

    public virtual Medicine? Medicine { get; set; }

    public virtual ResearchProject? Project { get; set; }

    public virtual ICollection<RegulatorySubmission> RegulatorySubmissions { get; set; } = new List<RegulatorySubmission>();

    public virtual Unit? StrengthUnit { get; set; }
}
