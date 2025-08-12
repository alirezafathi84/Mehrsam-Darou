using System;
using System.Collections.Generic;

namespace Mehrsam_Darou.Models;

public partial class MaterialRequest
{
    public Guid RequestId { get; set; }

    public string RequestNumber { get; set; } = null!;

    public string RequestTitle { get; set; } = null!;

    public Guid RequestTypeId { get; set; }

    public Guid CategoryId { get; set; }

    public Guid RequestedBy { get; set; }

    public string? Department { get; set; }

    public int PriorityLevel { get; set; }

    public string? Urgency { get; set; }

    public DateTime RequestDate { get; set; }

    public DateTime? RequiredDate { get; set; }

    public string? Description { get; set; }

    public string? Justification { get; set; }

    public decimal? TotalEstimatedCost { get; set; }

    public string? Currency { get; set; }

    public string? BudgetCode { get; set; }

    public string Status { get; set; } = null!;

    public string? CurrentStep { get; set; }

    public string WorkflowStage { get; set; } = null!;

    public string? ApprovalStatus { get; set; }

    public Guid? ApprovedBy { get; set; }

    public DateTime? ApprovalDate { get; set; }

    public decimal? ApprovedAmount { get; set; }

    public string? RejectionReason { get; set; }

    public Guid? SupplierSuggested { get; set; }

    public string? DeliveryLocation { get; set; }

    public string? SpecialInstructions { get; set; }

    public string? AttachmentsPath { get; set; }

    public bool IsUrgent { get; set; }

    public bool IsSubstituteAllowed { get; set; }

    public Guid? ProcessedBy { get; set; }

    public DateTime? ProcessedDate { get; set; }

    public DateTime? CompletionDate { get; set; }

    public string? Notes { get; set; }

    public bool IsActive { get; set; }

    public Guid CreatedBy { get; set; }

    public DateTime CreatedDate { get; set; }

    public Guid? LastModifiedBy { get; set; }

    public DateTime? LastModifiedDate { get; set; }

    public virtual User? ApprovedByNavigation { get; set; }

    public virtual RequestCategory Category { get; set; } = null!;

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual ICollection<MaterialRequestItem> MaterialRequestItems { get; set; } = new List<MaterialRequestItem>();

    public virtual User? ProcessedByNavigation { get; set; }

    public virtual ICollection<RequestApproval> RequestApprovals { get; set; } = new List<RequestApproval>();

    public virtual RequestType RequestType { get; set; } = null!;

    public virtual ICollection<RequestWorkflowHistory> RequestWorkflowHistories { get; set; } = new List<RequestWorkflowHistory>();

    public virtual User RequestedByNavigation { get; set; } = null!;

    public virtual Supplier? SupplierSuggestedNavigation { get; set; }
}
