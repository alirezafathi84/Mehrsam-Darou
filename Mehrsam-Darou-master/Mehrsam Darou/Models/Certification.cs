using System;
using System.Collections.Generic;

namespace Mehrsam_Darou.Models;

public partial class Certification
{
    public Guid CertificationId { get; set; }

    public string CertificationCode { get; set; } = null!;

    public string CertificationName { get; set; } = null!;

    public string? CertificationType { get; set; }

    public string? CertificationCategory { get; set; }

    public string? IssuingAuthority { get; set; }

    public string? IssuingCountry { get; set; }

    public string? Description { get; set; }

    public string? Requirements { get; set; }

    public DateOnly? IssueDate { get; set; }

    public DateOnly? ExpiryDate { get; set; }

    public int? RenewalPeriodMonths { get; set; }

    public string? CertificationStatus { get; set; }

    public string? CertificateNumber { get; set; }

    public string? AccreditationBody { get; set; }

    public string? ScopeOfCertification { get; set; }

    public Guid? RelatedMedicineId { get; set; }

    public string? RelatedFacility { get; set; }

    public string? ComplianceStandard { get; set; }

    public int? AuditFrequencyMonths { get; set; }

    public DateOnly? NextAuditDate { get; set; }

    public DateOnly? LastAuditDate { get; set; }

    public string? AuditResults { get; set; }

    public bool? CorrectiveActionsRequired { get; set; }

    public string? ResponsiblePerson { get; set; }

    public string? ContactEmail { get; set; }

    public string? ContactPhone { get; set; }

    public string? DocumentPath { get; set; }

    public decimal? Cost { get; set; }

    public string? Currency { get; set; }

    public int? RenewalReminderDays { get; set; }

    public int? PriorityLevel { get; set; }

    public string? Notes { get; set; }

    public bool? IsActive { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime CreatedDate { get; set; }

    public Guid? LastModifiedBy { get; set; }

    public DateTime? LastModifiedDate { get; set; }

    public virtual User? CreatedByNavigation { get; set; }

    public virtual User? LastModifiedByNavigation { get; set; }

    public virtual Medicine? RelatedMedicine { get; set; }
}
