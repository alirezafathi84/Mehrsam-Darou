using System;
using System.Collections.Generic;

namespace Mehrsam_Darou.Models;

public partial class DevelopmentBatch
{
    public Guid BatchId { get; set; }

    public string BatchCode { get; set; } = null!;

    public Guid FormulationId { get; set; }

    public Guid? ProjectId { get; set; }

    public string? BatchType { get; set; }

    public decimal BatchSize { get; set; }

    public Guid BatchUnitId { get; set; }

    public DateOnly ManufactureDate { get; set; }

    public DateOnly? ExpiryDate { get; set; }

    public string? ManufacturingLocation { get; set; }

    public Guid? OperatorId { get; set; }

    public Guid? SupervisorId { get; set; }

    public string? EquipmentUsed { get; set; }

    public string? ProcessParameters { get; set; }

    public decimal? YieldPercentage { get; set; }

    public string? QualityStatus { get; set; }

    public string? StorageLocation { get; set; }

    public string? StorageConditions { get; set; }

    public string? CostAnalysis { get; set; }

    public string? Observations { get; set; }

    public string? LessonsLearned { get; set; }

    public bool? ApprovedForNextStage { get; set; }

    public string? Notes { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime CreatedDate { get; set; }

    public bool? IsActive { get; set; }

    public virtual Unit BatchUnit { get; set; } = null!;

    public virtual Formulation Formulation { get; set; } = null!;

    public virtual ResearchProject? Project { get; set; }
}
