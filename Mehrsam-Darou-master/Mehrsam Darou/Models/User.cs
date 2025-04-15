using System;
using System.Collections.Generic;

namespace Mehrsam_Darou.Models;

public partial class User
{
    public Guid Id { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? Username { get; set; }

    public string? Password { get; set; }

    public DateTime? DateCreated { get; set; }

    public DateTime? DateModified { get; set; }

    public int Version { get; set; }

    public Guid? TeamId { get; set; }

    public string? AvatarImg { get; set; }

    public virtual ICollection<ChatMessage> ChatMessageReceivers { get; set; } = new List<ChatMessage>();

    public virtual ICollection<ChatMessage> ChatMessageSenders { get; set; } = new List<ChatMessage>();

    public virtual Team? Team { get; set; }

    public virtual ICollection<UserEnterLog> UserEnterLogs { get; set; } = new List<UserEnterLog>();

    public virtual UserStatus? UserStatus { get; set; }
}
