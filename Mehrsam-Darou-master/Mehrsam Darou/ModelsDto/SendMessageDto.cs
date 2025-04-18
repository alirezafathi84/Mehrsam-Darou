using System;

public class SendMessageDto
{
    public Guid ReceiverId { get; set; }
    public string Content { get; set; }
    public string Attachments { get; set; }
}
