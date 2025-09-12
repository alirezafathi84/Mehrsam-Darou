using System;
using System.Collections.Generic;

namespace Mehrsam_Darou.Models;

public partial class Project
{
    public Guid ProjectId { get; set; }

    public string ProjectCode { get; set; } = null!;

    public string ProjectName { get; set; } = null!;

    public string? Description { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public string? Status { get; set; }

    public bool? IsActive { get; set; }

    public DateTime CreatedDate { get; set; }

    public virtual ICollection<MaterialRequest> MaterialRequests { get; set; } = new List<MaterialRequest>();
}
