using System;
using System.Collections.Generic;

namespace Mehrsam_Darou.Models;

public partial class VwAccountBalance
{
    public Guid AccountId { get; set; }

    public string AccountCode { get; set; } = null!;

    public string AccountName { get; set; } = null!;

    public string AccountType { get; set; } = null!;

    public string NormalBalance { get; set; } = null!;

    public decimal? TotalDebit { get; set; }

    public decimal? TotalCredit { get; set; }

    public decimal? Balance { get; set; }
}
