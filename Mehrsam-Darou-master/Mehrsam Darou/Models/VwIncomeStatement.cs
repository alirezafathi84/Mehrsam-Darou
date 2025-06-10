using System;
using System.Collections.Generic;

namespace Mehrsam_Darou.Models;

public partial class VwIncomeStatement
{
    public string AccountType { get; set; } = null!;

    public string AccountCode { get; set; } = null!;

    public string AccountName { get; set; } = null!;

    public decimal? Amount { get; set; }
}
