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

        [HttpPost("mark-as-read/{notificationId}")]
        public async Task<IActionResult> MarkAsRead(Guid notificationId)
        {
            // Lấy UserId từ token
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
                return Unauthorized("Invalid or missing user ID");

            var success = await _notificationService.MarkAsReadAsync(notificationId, userId);
            if (!success)
                return NotFound("Notification not found or access denied");

            return Ok(new { success = true });
        }

        [HttpGet("my-notification/{notificationId}")]
        public async Task<IActionResult> GetNotificationDetail(Guid notificationId)
        {
            // Lấy UserId từ token
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
                return Unauthorized("Invalid or missing user ID");

            var notification = await _notificationService.GetNotificationDetailAsync(notificationId, userId);

            if (notification == null)
                return NotFound("Notification not found or access denied");

            return Ok(notification);
        }

    }

}
