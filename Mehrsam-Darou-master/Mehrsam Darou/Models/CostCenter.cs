using System;
using System.Collections.Generic;

namespace Mehrsam_Darou.Models;

public partial class CostCenter
{
    public Guid CostCenterId { get; set; }

    public string CostCenterCode { get; set; } = null!;

    public string CostCenterName { get; set; } = null!;

    public string? Description { get; set; }

    public Guid? ParentCostCenterId { get; set; }

    public bool? IsActive { get; set; }

    public DateTime CreatedDate { get; set; }

    public virtual ICollection<CostCenter> InverseParentCostCenter { get; set; } = new List<CostCenter>();

    public virtual ICollection<MaterialRequest> MaterialRequests { get; set; } = new List<MaterialRequest>();

    public virtual CostCenter? ParentCostCenter { get; set; }
}
