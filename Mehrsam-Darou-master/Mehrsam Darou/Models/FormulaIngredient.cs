using System;
using System.Collections.Generic;

namespace Mehrsam_Darou.Models;

public partial class FormulaIngredient
{
    public Guid IngredientId { get; set; }

    public Guid FormulaId { get; set; }

    public Guid MaterialId { get; set; }

    public string IngredientName { get; set; } = null!;

    public string? FunctionType { get; set; }

    public decimal Quantity { get; set; }

    public Guid UnitId { get; set; }

    public decimal? Percentage { get; set; }

    public string? Specification { get; set; }

    public string? SupplierPreference { get; set; }

    public string? AlternativeMaterials { get; set; }

    public string? CriticalQualityAttributes { get; set; }

    public string? HandlingPrecautions { get; set; }

    public string? StorageRequirements { get; set; }

    public decimal? CostPerUnit { get; set; }

    public decimal? TotalCost { get; set; }

    public int? SequenceOrder { get; set; }

    public string? AdditionMethod { get; set; }

    public string? ProcessingNotes { get; set; }

    public string? QualityControl { get; set; }

    public string? RegulatoryStatus { get; set; }

    public string? SafetyData { get; set; }

    public string? EnvironmentalImpact { get; set; }

    public string? Notes { get; set; }

    public bool? IsActive { get; set; }

    public DateTime CreatedDate { get; set; }

    public virtual Formula Formula { get; set; } = null!;

    public virtual RawMaterial Material { get; set; } = null!;

    public virtual Unit Unit { get; set; } = null!;
}
