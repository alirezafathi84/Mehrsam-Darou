using System;
using System.Collections.Generic;

namespace Mehrsam_Darou.Models;

public partial class Setting
{
    public Guid Id { get; set; }

    public int? NumberPerPage { get; set; }

    public bool? DefaultColor { get; set; }

    public bool? IsNavDark { get; set; }

    public bool? IsMenuDark { get; set; }
}
