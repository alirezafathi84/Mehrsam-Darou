using System;
using System.Collections.Generic;

namespace Mehrsam_Darou.Models;

public partial class ProductionOrderStep
{
    public Guid OrderStepId { get; set; }

    public Guid OrderId { get; set; }

    public Guid StepId { get; set; }

    public DateTime? PlannedStart { get; set; }

    public DateTime? ActualStart { get; set; }

    public DateTime? ActualEnd { get; set; }

    public string? Status { get; set; }

    public string? Notes { get; set; }

    public virtual ProductionOrder Order { get; set; } = null!;

    public virtual ProductionStep Step { get; set; } = null!;
}
