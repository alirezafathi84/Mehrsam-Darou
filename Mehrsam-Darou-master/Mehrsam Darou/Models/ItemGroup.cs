using System;
using System.Collections.Generic;

namespace Mehrsam_Darou.Models;

public partial class ItemGroup
{
    public Guid ItemGroupId { get; set; }

    public string GroupCode { get; set; } = null!;

    public string GroupName { get; set; } = null!;

    public string? Description { get; set; }

    public Guid? ParentGroupId { get; set; }

    public bool? IsActive { get; set; }

    public DateTime CreatedDate { get; set; }

    public virtual ICollection<ItemGroup> InverseParentGroup { get; set; } = new List<ItemGroup>();

    public virtual ICollection<MaterialRequestItem> MaterialRequestItems { get; set; } = new List<MaterialRequestItem>();

    public virtual ItemGroup? ParentGroup { get; set; }
}
