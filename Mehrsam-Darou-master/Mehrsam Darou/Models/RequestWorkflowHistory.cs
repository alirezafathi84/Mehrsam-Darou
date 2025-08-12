using System;
using System.Collections.Generic;

namespace Mehrsam_Darou.Models;

public partial class RequestWorkflowHistory
{
    public Guid WorkflowId { get; set; }

    public Guid RequestId { get; set; }

    public string Stage { get; set; } = null!;

    public string Status { get; set; } = null!;

    public string? ActionTaken { get; set; }

    public string? Comments { get; set; }

    public Guid ProcessedBy { get; set; }

    public DateTime ProcessedDate { get; set; }

    public string? NextStage { get; set; }

    public Guid? AssignedTo { get; set; }

    public decimal? DurationHours { get; set; }

    public bool IsActive { get; set; }

    public virtual User? AssignedToNavigation { get; set; }

    public virtual User ProcessedByNavigation { get; set; } = null!;

    public virtual MaterialRequest Request { get; set; } = null!;
}
