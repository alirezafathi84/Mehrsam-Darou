using System;
using System.Collections.Generic;

namespace Mehrsam_Darou.Models;

public partial class PersianDateConverter
{
    public DateOnly GregorianDate { get; set; }

    public string PersianDate { get; set; } = null!;

    public int PersianYear { get; set; }

    public int PersianMonth { get; set; }

    public int PersianDay { get; set; }

    public bool IsHoliday { get; set; }

    public bool IsWorkingDay { get; set; }
}
