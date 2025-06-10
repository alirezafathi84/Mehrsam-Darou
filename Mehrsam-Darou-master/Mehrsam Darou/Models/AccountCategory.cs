using System;
using System.Collections.Generic;

namespace Mehrsam_Darou.Models;

public partial class AccountCategory
{
    public Guid CategoryId { get; set; }

    public string CategoryCode { get; set; } = null!;

    public string CategoryName { get; set; } = null!;

    public string CategoryType { get; set; } = null!;

    public Guid? ParentCategoryId { get; set; }

    public int Level { get; set; }

    public bool? IsActive { get; set; }

    public DateTime CreatedDate { get; set; }

    public virtual ICollection<ChartOfAccount> ChartOfAccounts { get; set; } = new List<ChartOfAccount>();

    public virtual ICollection<AccountCategory> InverseParentCategory { get; set; } = new List<AccountCategory>();

    public virtual AccountCategory? ParentCategory { get; set; }
}
