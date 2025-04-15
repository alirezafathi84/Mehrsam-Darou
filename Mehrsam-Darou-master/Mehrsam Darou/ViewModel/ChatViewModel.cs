namespace Mehrsam_Darou.ViewModel
{
    public class ChatViewModel
    {
        public Guid CurrentUserId { get; set; }
        public List<OnlineUserDto> OnlineUsers { get; set; } = new List<OnlineUserDto>();
        public List<RecentChatDto> RecentChats { get; set; } = new List<RecentChatDto>();
        public List<ContactDto> Contacts { get; set; } = new List<ContactDto>();
        public CurrentChatDto? CurrentChat { get; set; }
    }

    public class OnlineUserDto
    {
        public Guid Id { get; set; }
        public string Username { get; set; }
        public string Name { get; set; }
        public string? AvatarImg { get; set; }
        public string Status { get; set; }
        public DateTime LastSeen { get; set; }
    }

    public class RecentChatDto
    {
        public Guid ContactId { get; set; }
        public string ContactName { get; set; }
        public string ContactAvatar { get; set; }
        public string LastMessagePreview { get; set; }
        public string LastMessageTime { get; set; }
        public bool IsRead { get; set; }
        public bool LastMessageIsMine { get; set; }
        public bool IsActive { get; set; }
    }

    public class ContactDto
    {
        public Guid Id { get; set; }
        public string FullName { get; set; }
        public string? AvatarImg { get; set; }
        public string? StatusMessage { get; set; }
        public string Status { get; set; }
    }

    public class CurrentChatDto
    {
        public Guid ContactId { get; set; }
        public string ContactName { get; set; }
        public string ContactAvatar { get; set; }
        public string? ContactPhone { get; set; }
        public string? ContactLocation { get; set; }
        public string? ContactLanguages { get; set; }
        public List<string> ContactGroups { get; set; } = new List<string>();
        public List<ChatMessageDto> Messages { get; set; } = new List<ChatMessageDto>();
    }

    public class ChatMessageDto
    {
        public Guid Id { get; set; }
        public string Content { get; set; }
        public DateTime SentAt { get; set; }
        public bool IsRead { get; set; }
        public bool IsMine { get; set; }
        public string? Attachments { get; set; }
    }
}
