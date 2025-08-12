using System;
using System.Collections.Generic;

namespace Mehrsam_Darou.Models;

public partial class RequestCategory
{
    public Guid CategoryId { get; set; }

    public string CategoryName { get; set; } = null!;

    public string CategoryType { get; set; } = null!;

    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedDate { get; set; }

    public virtual ICollection<MaterialRequest> MaterialRequests { get; set; } = new List<MaterialRequest>();

    public virtual ICollection<RequestType> RequestTypes { get; set; } = new List<RequestType>();
}
