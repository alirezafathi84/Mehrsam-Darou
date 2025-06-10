using System;
using System.Collections.Generic;

namespace Mehrsam_Darou.Models;

public partial class FormulationIngredient
{
    public Guid IngredientId { get; set; }

    public Guid FormulationId { get; set; }

    public Guid MaterialId { get; set; }

    public string? IngredientType { get; set; }

    public decimal QuantityPerUnit { get; set; }

    public Guid UnitId { get; set; }

    public decimal? Percentage { get; set; }

    public string? FunctionDescription { get; set; }

    public bool? CriticalParameter { get; set; }

    public decimal? AcceptableRangeMin { get; set; }

    public decimal? AcceptableRangeMax { get; set; }

    public string? SupplierSpecification { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedDate { get; set; }

    public bool? IsActive { get; set; }

    public virtual Formulation Formulation { get; set; } = null!;

    public virtual RawMaterial Material { get; set; } = null!;

    public virtual Unit Unit { get; set; } = null!;
}
