using System;
using System.Collections.Generic;

namespace Mehrsam_Darou.Models;

public partial class LaboratoryTest
{
    public Guid TestId { get; set; }

    public string TestCode { get; set; } = null!;

    public string TestName { get; set; } = null!;

    public string? TestType { get; set; }

    public Guid? ProjectId { get; set; }

    public Guid? FormulationId { get; set; }

    public string? BatchNumber { get; set; }

    public string? TestMethod { get; set; }

    public string? Specification { get; set; }

    public DateOnly TestDate { get; set; }

    public Guid? TechnicianId { get; set; }

    public Guid? SupervisorId { get; set; }

    public string? EquipmentUsed { get; set; }

    public string? SampleInfo { get; set; }

    public string? TestConditions { get; set; }

    public string? Results { get; set; }

    public string? PassFail { get; set; }

    public string? DeviationNotes { get; set; }

    public bool? RetestRequired { get; set; }

    public Guid? ApprovedBy { get; set; }

    public DateTime? ApprovalDate { get; set; }

    public string? CertificateNumber { get; set; }

    public string? Notes { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime CreatedDate { get; set; }

    public bool? IsActive { get; set; }

    public virtual Formulation? Formulation { get; set; }

    public virtual ResearchProject? Project { get; set; }
}
