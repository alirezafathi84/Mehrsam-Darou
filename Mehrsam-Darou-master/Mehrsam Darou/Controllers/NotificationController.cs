
using Mehrsam_Darou.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;

namespace Mehrsam_Darou.Controllers
{
    public class NotificationController : BaseController
    {
        private readonly DarouAppContext _context;


        public NotificationController(DarouAppContext context) : base(context)
        {
            _context = context; 
        }



        //[HttpPost]
        public async Task<IActionResult> Noti(Guid notificationId)
        {
         
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification != null) { 
            
            if (notification.Type == "chat")
                {
                    await MarkNotificationSeen(notificationId,"chat");


               



                    return RedirectToAction("Chat", "Chat", new { contactId = notification.RelatedId });
                }







            }



        return View(notification);
        }


        [HttpPost]
        public async Task<IActionResult> InsertNotification([FromBody] Notification model)
        {
            if (model == null)
                return BadRequest("Invalid notification data.");

            model.Id = Guid.NewGuid();              // Generate new GUID if not provided
            model.CreatedDate = DateTime.Now;    // Set creation date to now (UTC)
            model.Seen = false;                      // Default seen to false

            _context.Notifications.Add(model);
            await _context.SaveChangesAsync();

            return Ok(model);
        }

        [HttpPost]
        public async Task<IActionResult> MarkNotificationSeen(Guid notificationId, string type)
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification == null)
                return NotFound();

            switch (type)
            {
                case "chat":


                    var notifications = await _context.Notifications
  .Where(m => m.RelatedId.Equals(notification.RelatedId) && m.Type == "chat" && !m.Seen)
  .ToListAsync();

                    if (notifications.Any())
                    {
                        foreach (var n in notifications)
                            n.Seen = true;
                        await _context.SaveChangesAsync();

                    }



                    break;



                default:
                    break;
            }

 



            return Ok("Notification marked as seen.");
        }





    }
}
