using System;
using System.Collections.Generic;

namespace Mehrsam_Darou.Models;

public partial class IntellectualProperty
{
    public Guid IpId { get; set; }

    public string IpCode { get; set; } = null!;

    public string Title { get; set; } = null!;

    public string? IpType { get; set; }

    public Guid? ProjectId { get; set; }

    public Guid? FormulationId { get; set; }

    public string? ApplicationNumber { get; set; }

    public DateOnly? ApplicationDate { get; set; }

    public DateOnly? PriorityDate { get; set; }

    public string? PublicationNumber { get; set; }

    public DateOnly? PublicationDate { get; set; }

    public string? GrantNumber { get; set; }

    public DateOnly? GrantDate { get; set; }

    public DateOnly? ExpiryDate { get; set; }

    public string? Inventors { get; set; }

    public string? Assignee { get; set; }

    public string? Countries { get; set; }

    public string? Status { get; set; }

    public string? Abstract { get; set; }

    public string? Claims { get; set; }

    public string? FilePath { get; set; }

    public string? AttorneyInfo { get; set; }

    public decimal? FeesPaid { get; set; }

    public DateOnly? MaintenanceDueDate { get; set; }

    public string? Notes { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime CreatedDate { get; set; }

    public bool? IsActive { get; set; }

    public virtual Formulation? Formulation { get; set; }

    public virtual ResearchProject? Project { get; set; }
}
