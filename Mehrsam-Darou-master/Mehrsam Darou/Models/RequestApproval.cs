using System;
using System.Collections.Generic;

namespace Mehrsam_Darou.Models;

public partial class RequestApproval
{
    public Guid ApprovalId { get; set; }

    public Guid RequestId { get; set; }

    public string ApprovalLevel { get; set; } = null!;

    public Guid ApproverId { get; set; }

    public string ApprovalStatus { get; set; } = null!;

    public DateTime? ApprovalDate { get; set; }

    public string? Comments { get; set; }

    public decimal? ApprovedAmount { get; set; }

    public string? Conditions { get; set; }

    public int SequenceOrder { get; set; }

    public bool IsRequired { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedDate { get; set; }

    public virtual User Approver { get; set; } = null!;

    public virtual MaterialRequest Request { get; set; } = null!;
}
