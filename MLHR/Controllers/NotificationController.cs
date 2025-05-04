using BusinessObject.Models;
using Microsoft.AspNetCore.Mvc;
using Services.IService;
using System.Security.Claims;

namespace MLHR.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpGet("my-notification")]
        public async Task<IActionResult> GetMyNotifications()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                return Unauthorized("User not authenticated.");

            var userId = Guid.Parse(userIdClaim.Value);
            var notifications = await _notificationService.GetNotificationsForUserAsync(userId);

            return Ok(notifications);
        }
    }

}
