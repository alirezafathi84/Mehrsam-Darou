using System;
using System.Collections.Generic;

namespace Mehrsam_Darou.Models;

public partial class Medicine
{
    public Guid Id { get; set; }

    public string? MedicineName { get; set; }

    public string? MedicineCode { get; set; }

    public int? Priority { get; set; }

    public DateTime? CreatedDate { get; set; }

    public virtual ICollection<Bom> Boms { get; set; } = new List<Bom>();
}
