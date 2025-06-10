using System;
using System.Collections.Generic;

namespace Mehrsam_Darou.Models;

public partial class PaymentTransaction
{
    public Guid TransactionId { get; set; }

    public string TransactionNumber { get; set; } = null!;

    public string TransactionType { get; set; } = null!;

    public Guid PaymentMethodId { get; set; }

    public Guid? RelatedInvoiceId { get; set; }

    public string? InvoiceType { get; set; }

    public Guid? CustomerId { get; set; }

    public Guid? SupplierId { get; set; }

    public decimal Amount { get; set; }

    public string? Currency { get; set; }

    public DateOnly TransactionDate { get; set; }

    public DateOnly? DueDate { get; set; }

    public string? ReferenceNumber { get; set; }

    public string? BankName { get; set; }

    public string? AccountNumber { get; set; }

    public string? Status { get; set; }

    public string? Description { get; set; }

    public string? AttachmentPath { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime CreatedDate { get; set; }

    public Guid? ApprovedBy { get; set; }

    public DateTime? ApprovedDate { get; set; }

    public virtual User? ApprovedByNavigation { get; set; }

    public virtual User? CreatedByNavigation { get; set; }

    public virtual Customer? Customer { get; set; }

    public virtual PaymentMethod PaymentMethod { get; set; } = null!;

    public virtual Supplier? Supplier { get; set; }
}
