using System;
using System.Collections.Generic;

namespace Mehrsam_Darou.Models;

public partial class ChatMessage
{
    public Guid Id { get; set; }

    public string Content { get; set; } = null!;

    public DateTime SentAt { get; set; }

    public bool IsRead { get; set; }

    public Guid SenderId { get; set; }

    public Guid ReceiverId { get; set; }

    public string? Attachments { get; set; }

    public virtual User Receiver { get; set; } = null!;

    public virtual User Sender { get; set; } = null!;
}
