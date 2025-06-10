using System;
using System.Collections.Generic;

namespace Mehrsam_Darou.Models;

public partial class ChartOfAccount
{
    public Guid AccountId { get; set; }

    public string AccountCode { get; set; } = null!;

    public string AccountName { get; set; } = null!;

    public string AccountType { get; set; } = null!;

    public Guid CategoryId { get; set; }

    public Guid? ParentAccountId { get; set; }

    public int AccountLevel { get; set; }

    public string NormalBalance { get; set; } = null!;

    public bool? IsActive { get; set; }

    public bool? IsSystem { get; set; }

    public string? Description { get; set; }

    public DateTime CreatedDate { get; set; }

    public virtual AccountCategory Category { get; set; } = null!;

    public virtual ICollection<ChartOfAccount> InverseParentAccount { get; set; } = new List<ChartOfAccount>();

    public virtual ICollection<JournalEntryDetail> JournalEntryDetails { get; set; } = new List<JournalEntryDetail>();

    public virtual ChartOfAccount? ParentAccount { get; set; }
}
