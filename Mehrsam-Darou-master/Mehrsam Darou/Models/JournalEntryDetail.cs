using System;
using System.Collections.Generic;

namespace Mehrsam_Darou.Models;

public partial class JournalEntryDetail
{
    public Guid DetailId { get; set; }

    public Guid EntryId { get; set; }

    public Guid AccountId { get; set; }

    public decimal? DebitAmount { get; set; }

    public decimal? CreditAmount { get; set; }

    public string? Description { get; set; }

    public string? ReferenceType { get; set; }

    public Guid? ReferenceId { get; set; }

    public virtual ChartOfAccount Account { get; set; } = null!;

    public virtual JournalEntry Entry { get; set; } = null!;
}
