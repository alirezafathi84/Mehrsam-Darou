using System;
using System.Collections.Generic;

namespace Mehrsam_Darou.Models;

public partial class VwTrialBalance
{
    public string AccountCode { get; set; } = null!;

    public string AccountName { get; set; } = null!;

    public string AccountType { get; set; } = null!;

    public decimal? DebitBalance { get; set; }

    public decimal? CreditBalance { get; set; }
}
