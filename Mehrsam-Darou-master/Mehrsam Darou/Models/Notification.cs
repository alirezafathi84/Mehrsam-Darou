using System;
using System.Collections.Generic;

namespace Mehrsam_Darou.Models;

public partial class Notification
{
    public Guid Id { get; set; }

    public Guid? RelatedId { get; set; }

    public string? Type { get; set; }

    public bool Seen { get; set; }

    public string? Title { get; set; }

    public string? Message { get; set; }

    public DateTime CreatedDate { get; set; }

    public Guid? UserId { get; set; }

    public string? Img { get; set; }

    public virtual User? User { get; set; }
}
