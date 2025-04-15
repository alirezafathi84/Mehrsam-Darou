using Mehrsam_Darou.Models;
using Mehrsam_Darou.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Mehrsam_Darou.Controllers
{
    public class ChatController : BaseController
    {
        private readonly DarouAppContext _context;
        private readonly IHubContext<ChatHub> _hubContext;

        public ChatController(DarouAppContext context, IHubContext<ChatHub> hubContext) : base(context)
        {
            _context = context;
            _hubContext = hubContext;
        }


        [HttpPost("SendMessage")]
        public async Task<IActionResult> SendMessage([FromForm] SendMessageDto dto)
        {
            var user = await ValidateSessionAndGetUser();
            if (user == null) return Unauthorized();

            // Create the message
            var message = new ChatMessage
            {
                Id = Guid.NewGuid(),
                SenderId = user.Id,
                ReceiverId = dto.ReceiverId,
                Content = dto.Content,
                Attachments = dto.Attachments,
                SentAt = DateTime.Now,
                IsRead = false
            };

            _context.ChatMessages.Add(message);
            await _context.SaveChangesAsync();

            // Notify the receiver via SignalR
            await _hubContext.Clients.User(dto.ReceiverId.ToString()).SendAsync("ReceiveMessage", new
            {
                message.Id,
                message.Content,
                SenderId = message.SenderId,
                SenderName = user.FirstName + " " + user.LastName,
                SenderAvatar = user.AvatarImg,
                message.SentAt,
                message.Attachments,
                IsMine = false
            });

            return RedirectToAction("Chat","Chat", new { contactId = dto.ReceiverId });
            //return Ok(new { message.Id });
        }






        //[HttpGet("Chat")]
        public async Task<IActionResult> Chat(Guid? contactId)
        {
            var user = await ValidateSessionAndGetUser();
            if (user == null)
                return RedirectToAction("Login", "Client");

            // Update user status to "online"
            var status = await _context.UserStatuses.FindAsync(user.Id);
            if (status == null)
            {
                status = new UserStatus { UserId = user.Id, Status = "online", LastSeen = DateTime.Now };
                _context.UserStatuses.Add(status);
            }
            else
            {
                status.Status = "online";
                status.LastSeen = DateTime.Now;
            }


            if (contactId != null) 
            {
                var messages = await _context.ChatMessages
                .Where(m => m.SenderId == contactId && m.ReceiverId == user.Id && !m.IsRead)
                .ToListAsync();

                foreach (var message in messages)
                {
                    message.IsRead = true;
                }
            }        

            await _context.SaveChangesAsync();

            var threshold = DateTime.Now.AddMinutes(-30);
            var onlineUsers = await _context.UserStatuses
                .Where(us => us.LastSeen >= threshold)
                .Join(_context.Users,
                      us => us.UserId,
                      u => u.Id,
                      (us, u) => new OnlineUserDto
                      {
                          Id = u.Id,
                          Name = u.FirstName + " " + u.LastName,
                          Username = u.Username,
                          AvatarImg = u.AvatarImg,
                          Status = us.LastSeen >= DateTime.Now.AddMinutes(-5) ? "online" : "away",
                          LastSeen = us.LastSeen
                      })
                .OrderByDescending(u => u.LastSeen)
                .ToListAsync();

            var contactIds = await _context.ChatMessages
                .Where(m => m.SenderId == user.Id || m.ReceiverId == user.Id)
                .Select(m => m.SenderId == user.Id ? m.ReceiverId : m.SenderId)
                .Distinct()
                .ToListAsync();

            var contacts = await _context.Users
               // .Where(u => contactIds.Contains(u.Id))
                .Select(u => new ContactDto
                {
                    Id = u.Id,
                    FullName = u.FirstName + " " + u.LastName,
                    AvatarImg = u.AvatarImg,
                    StatusMessage = _context.UserStatuses.Where(s => s.UserId == u.Id).Select(s => s.Status).FirstOrDefault(),
                    Status = _context.UserStatuses.Where(s => s.UserId == u.Id && s.LastSeen >= threshold).Any() ? "online" : "offline"
                })
                .ToListAsync();

            var allMessages = await _context.ChatMessages
                .Where(m => m.SenderId == user.Id || m.ReceiverId == user.Id)
                .OrderByDescending(m => m.SentAt)
                .ToListAsync();

            var recentChats = allMessages
                .GroupBy(m => m.SenderId == user.Id ? m.ReceiverId : m.SenderId)
                .Select(g => g.First())
                .Join(_context.Users.AsEnumerable(),
                    m => m.SenderId == user.Id ? m.ReceiverId : m.SenderId,
                    u => u.Id,
                    (m, u) => new RecentChatDto
                    {
                        ContactId = u.Id,
                        ContactName = $"{u.FirstName} {u.LastName}",
                        ContactAvatar = u.AvatarImg ?? "",
                        LastMessagePreview = m.Content,
                        LastMessageTime = m.SentAt.ToShortTimeString(),
                        IsRead = m.IsRead,
                        LastMessageIsMine = m.SenderId == user.Id,
                        IsActive = contactId.HasValue && contactId.Value == u.Id // Set the active chat if the contact matches
                    })
                .ToList();

            // Fetch the current chat if a contactId is provided
            CurrentChatDto currentChat = null;
            if (contactId.HasValue)
            {
                var chatMessages = await _context.ChatMessages
                    .Where(m => (m.SenderId == user.Id && m.ReceiverId == contactId.Value) ||
                                (m.ReceiverId == user.Id && m.SenderId == contactId.Value))
                    .OrderBy(m => m.SentAt)
                    .Select(m => new ChatMessageDto
                    {
                        Id = m.Id,
                        Content = m.Content,
                        SentAt = m.SentAt,
                        IsRead = m.IsRead,
                        IsMine = m.SenderId == user.Id,
                        Attachments = m.Attachments
                    })
                    .ToListAsync();

                var contact = await _context.Users.FirstOrDefaultAsync(u => u.Id == contactId.Value);
                if (contact != null)
                {
                    currentChat = new CurrentChatDto
                    {
                        ContactId = contact.Id,
                        ContactName = contact.FirstName + " " + contact.LastName,
                        ContactAvatar = contact.AvatarImg ?? "",

                        ContactGroups = new List<string>(), // If you have group data, fill it here
                        Messages = chatMessages
                    };
                }
            }

            var model = new ChatViewModel
            {
                CurrentUserId = user.Id,
                OnlineUsers = onlineUsers,
                Contacts = contacts,
                RecentChats = recentChats,
                CurrentChat = currentChat
            };

            return View("Chat", model);
        }



        [HttpPost("DeleteMessage")]
        public async Task<IActionResult> DeleteMessage(Guid messageId)
        {
            var user = await ValidateSessionAndGetUser();
            if (user == null) return Unauthorized();

            var message = await _context.ChatMessages.FindAsync(messageId);
            if (message == null || (message.SenderId != user.Id && message.ReceiverId != user.Id))
                return BadRequest("Message not found or you don't have permission to delete it.");

            _context.ChatMessages.Remove(message);
            await _context.SaveChangesAsync();

            return Ok();
        }
    }

    public class SendMessageDto
    {
        public Guid ReceiverId { get; set; }
        public string Content { get; set; }
        public string Attachments { get; set; }
    }
}
