using System;
using System.Collections.Generic;

namespace Mehrsam_Darou.Models;

public partial class MedicineCategory
{
    public Guid CategoryId { get; set; }

    public string CategoryName { get; set; } = null!;

    public string? Description { get; set; }

    public bool? IsActive { get; set; }

    public virtual ICollection<Medicine> Medicines { get; set; } = new List<Medicine>();
}
