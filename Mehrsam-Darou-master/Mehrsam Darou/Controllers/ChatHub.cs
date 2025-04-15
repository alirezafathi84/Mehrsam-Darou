using Mehrsam_Darou.Models;
using Microsoft.AspNetCore.SignalR;
using System.Text.RegularExpressions;

public class ChatHub : Hub
{
    private readonly DarouAppContext _context;

    public ChatHub(DarouAppContext context)
    {
        _context = context;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.GetHttpContext().Request.Query["userId"];
        if (!string.IsNullOrEmpty(userId) && Guid.TryParse(userId, out var id))
        {
            // Add connection to group for the user
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{id}");

            // Update user status
            var status = await _context.UserStatuses.FindAsync(id);
            if (status == null)
            {
                status = new UserStatus { UserId = id, Status = "online" };
                _context.UserStatuses.Add(status);
            }
            else
            {
                status.Status = "online";
            }

            status.LastSeen = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Notify other users
            await Clients.Others.SendAsync("UserStatusChanged", id, "online");
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception exception)
    {
        var userId = Context.GetHttpContext().Request.Query["userId"];
        if (!string.IsNullOrEmpty(userId) && Guid.TryParse(userId, out var id))
        {
            // Remove connection from group
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user-{id}");

            // Update user status
            var status = await _context.UserStatuses.FindAsync(id);
            if (status != null)
            {
                status.Status = "offline";
                status.LastSeen = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // Notify other users
                await Clients.Others.SendAsync("UserStatusChanged", id, "offline");
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task SendTypingNotification(Guid senderId, Guid receiverId)
    {
        await Clients.Group($"user-{receiverId}").SendAsync("UserTyping", senderId);
    }
}