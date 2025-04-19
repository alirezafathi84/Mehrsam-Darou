using System;
using System.Collections.Generic;

namespace Mehrsam_Darou.Models;

public partial class Bom
{
    public Guid Id { get; set; }

    public string? BomName { get; set; }

    public Guid? MedicineId { get; set; }

    public virtual Medicine? Medicine { get; set; }
}
