using System;
using System.Collections.Generic;

namespace Mehrsam_Darou.Models;

public partial class RequestType
{
    public Guid TypeId { get; set; }

    public string TypeName { get; set; } = null!;

    public Guid CategoryId { get; set; }

    public bool RequiresApproval { get; set; }

    public string? ApprovalLevel { get; set; }

    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedDate { get; set; }

    public virtual RequestCategory Category { get; set; } = null!;

    public virtual ICollection<MaterialRequest> MaterialRequests { get; set; } = new List<MaterialRequest>();
}
