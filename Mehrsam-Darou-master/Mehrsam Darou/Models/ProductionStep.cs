using System;
using System.Collections.Generic;

namespace Mehrsam_Darou.Models;

public partial class ProductionStep
{
    public Guid StepId { get; set; }

    public string StepName { get; set; } = null!;

    public string? Description { get; set; }

    public int Sequence { get; set; }

    public bool? IsActive { get; set; }

    public virtual ICollection<ProductionOrderStep> ProductionOrderSteps { get; set; } = new List<ProductionOrderStep>();
}
