using Mehrsam_Darou.Models;
using Mehrsam_Darou.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

        // Helper: prepares the main viewmodel for any user/chat.
        private async Task<ChatViewModel> BuildChatViewModel(Guid currentUserId, Guid? contactId)
        {
            var now = DateTime.Now;
            var awayThreshold = now.AddMinutes(-15);
            var onlineThreshold = now.AddMinutes(-5);

            var allUsers = await _context.Users
                .OrderBy(u => u.FirstName)
                .ThenBy(u => u.LastName)
                .ToListAsync();

            var allLogs = await _context.UserEnterLogs
                .Where(log => log.CreatedDate >= awayThreshold)
                .OrderByDescending(log => log.CreatedDate)
                .ToListAsync();

            var latestLogByUser = allLogs
                .GroupBy(l => l.UserId)
                .ToDictionary(g => g.Key, g => g.First());

            var onlineUsers = allUsers
                .Where(u => latestLogByUser.ContainsKey(u.Id))
                .Select(u =>
                {
                    var log = latestLogByUser[u.Id];
                    return new OnlineUserDto
                    {
                        Id = u.Id,
                        Name = u.FirstName + " " + u.LastName,
                        Username = u.Username,
                        AvatarImg = u.AvatarImg,
                        Status = log.CreatedDate >= onlineThreshold ? "آنلاین" : "خارج از دسترس",
                        LastSeen = (DateTime)log.CreatedDate
                    };
                })
                .GroupBy(u => u.Id).Select(g => g.First())
                .OrderByDescending(u => u.LastSeen)
                .ToList();

            var contacts = allUsers.Select(u =>
            {
                UserEnterLog lastLog = null;
                latestLogByUser.TryGetValue(u.Id, out lastLog);
                string status = "آفلاین";
                string statusMsg = null;
                if (lastLog != null)
                {
                    status = lastLog.CreatedDate >= onlineThreshold ? "آنلاین" :
                             lastLog.CreatedDate >= awayThreshold ? "خارج از دسترس" : "آفلاین";
                    statusMsg = lastLog.Status;
                }
                return new ContactDto
                {
                    Id = u.Id,
                    FullName = u.FirstName + " " + u.LastName,
                    AvatarImg = u.AvatarImg,
                    StatusMessage = statusMsg,
                    Status = status
                };
            }).ToList();

            var allMessages = await _context.ChatMessages
                .Where(m => m.SenderId == currentUserId || m.ReceiverId == currentUserId)
                .OrderByDescending(m => m.SentAt)
                .ToListAsync();

            var recentChats = allMessages
                .GroupBy(m => m.SenderId == currentUserId ? m.ReceiverId : m.SenderId)
                .Select(g => g.First())
                .Join(allUsers,
                    m => m.SenderId == currentUserId ? m.ReceiverId : m.SenderId,
                    u => u.Id,
                    (m, u) => new RecentChatDto
                    {
                        ContactId = u.Id,
                        ContactName = $"{u.FirstName} {u.LastName}",
                        ContactAvatar = u.AvatarImg ?? "",
                        LastMessagePreview = m.Content,
                        LastMessageTime = m.SentAt,
                        IsRead = m.IsRead,
                        LastMessageIsMine = m.SenderId == currentUserId,
                        IsActive = contactId.HasValue && contactId.Value == u.Id
                    })
                .ToList();

            CurrentChatDto currentChat = null;
            if (contactId.HasValue)
            {
                // --- Mark unread incoming messages as read ---
                var unreadMessages = await _context.ChatMessages
                    .Where(m => m.SenderId == contactId.Value && m.ReceiverId == currentUserId && !m.IsRead)
                    .ToListAsync();

                if (unreadMessages.Any())
                {
                    foreach (var m in unreadMessages)
                        m.IsRead = true;
                    await _context.SaveChangesAsync();

                }

                // --- Now load all chat messages for this conversation ---
                var chatMessages = await _context.ChatMessages
                    .Where(m => (m.SenderId == currentUserId && m.ReceiverId == contactId.Value) ||
                                (m.ReceiverId == currentUserId && m.SenderId == contactId.Value))
                    .OrderBy(m => m.SentAt)
                    .Select(m => new ChatMessageDto
                    {
                        Id = m.Id,
                        Content = m.Content,
                        SentAt = m.SentAt,
                        IsRead = m.IsRead,
                        IsMine = m.SenderId == currentUserId,
                        Attachments = m.Attachments
                    })
                    .ToListAsync();

                var contact = allUsers.FirstOrDefault(u => u.Id == contactId.Value);
                if (contact != null)
                {
                    currentChat = new CurrentChatDto
                    {
                        ContactId = contact.Id,
                        ContactName = contact.FirstName + " " + contact.LastName,
                        ContactAvatar = contact.AvatarImg ?? "",
                        ContactGroups = new List<string>(),
                        Messages = chatMessages
                    };
                }



            }

     

            return new ChatViewModel
            {
                CurrentUserId = currentUserId,
                OnlineUsers = onlineUsers,
                Contacts = contacts,
                RecentChats = recentChats,
                CurrentChat = currentChat
            };
        }



        // -- FULL PAGE --
        public async Task<IActionResult> Chat(Guid? contactId)
        {
            var user = await ValidateSessionAndGetUser();
            if (user == null)
                return RedirectToAction("Login", "Client");

            // Add online log
            _context.UserEnterLogs.Add(new UserEnterLog
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                CreatedDate = DateTime.Now,
                Status = "آنلاین"
            });
            await _context.SaveChangesAsync();

            // Mark unread as read if viewing contact
            if (contactId != null)
            {
                var messages = await _context.ChatMessages
                    .Where(m => m.SenderId == contactId && m.ReceiverId == user.Id && !m.IsRead)
                    .ToListAsync();
                foreach (var message in messages)
                    message.IsRead = true;
                await _context.SaveChangesAsync();
            }

            var model = await BuildChatViewModel(user.Id, contactId);
            return View(model);
        }

        // -- PARTIAL: Sidebar (ONLINE USERS/RECENT/CONTACTS) --
        public async Task<IActionResult> OnlineUsersPartial(Guid? contactId)
        {
            var user = await ValidateSessionAndGetUser();
            if (user == null) return Unauthorized();
            var model = await BuildChatViewModel(user.Id, contactId);
            return PartialView("_OnlineUsersPartial", model);
        }

        // -- PARTIAL: Main Chat/Message Area --
        public async Task<IActionResult> CurrentChatPartial(Guid? contactId)
        {
            var user = await ValidateSessionAndGetUser();

            // Mark unread as read
            if (contactId != null)
            {
                var messages = await _context.ChatMessages
                    .Where(m => m.SenderId == contactId && m.ReceiverId == user.Id && !m.IsRead)
                    .ToListAsync();
                foreach (var message in messages)
                    message.IsRead = true;
                await _context.SaveChangesAsync();
            }
            var model = await BuildChatViewModel(user.Id, contactId);
            return PartialView("_CurrentChatPartial", model);
        }

        // SEND MESSAGE (by form POST)
        [HttpPost("SendMessage")]
        public async Task<IActionResult> SendMessage([FromForm] SendMessageDto dto)
        {
            try
            {
                var user = await ValidateSessionAndGetUser();
       

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

            //// SignalR (optional, for real-time)
            //await _hubContext.Clients.User(dto.ReceiverId.ToString()).SendAsync("ReceiveMessage", new
            //{
            //    message.Id,
            //    message.Content,
            //    SenderId = message.SenderId,
            //    SenderName = user.FirstName + " " + user.LastName,
            //    SenderAvatar = user.AvatarImg,
            //    message.SentAt,
            //    message.Attachments,
            //    IsMine = false
            //});
            // Call SetForNoti, ignore all errors
       
                await Helper.Helper.SetForNoti(_context, dto.ReceiverId);

                return RedirectToAction("Chat", "Chat", new { contactId = dto.ReceiverId });
            }
         
                        catch (Exception ex)
            {
                return RedirectToAction("Chat", "Chat", new { contactId = new Guid("00000000-0000-0000-0000-000000000000") });

            }
        }
        
        

        // DELETE MESSAGE (by POST)
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
    // DTO for Sending Messages

}
