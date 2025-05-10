using Microsoft.AspNetCore.Mvc;
using Services.IService;

namespace MLHR.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;

        public ChatController(IChatService chatService)
        {
            _chatService = chatService;
        }

        [HttpGet("messages")]
        public async Task<IActionResult> GetAllMessages()
        {
            try
            {
                var messages = await _chatService.GetAllMessagesAsync();
                return Ok(messages);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"❌ Error in GetAllMessages: {ex.Message}");
            }
        }

        [HttpGet("messages/sender/{senderId}")]
        public async Task<IActionResult> GetMessagesBySender(Guid senderId)
        {
            try
            {
                var messages = await _chatService.GetMessagesBySenderAsync(senderId);
                return Ok(messages);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"❌ Error in GetMessagesBySender: {ex.Message}");
            }
        }

        [HttpGet("messages/receiver/{receiverId}")]
        public async Task<IActionResult> GetMessagesByReceiver(Guid receiverId)
        {
            try
            {
                var messages = await _chatService.GetMessagesByReceiverAsync(receiverId);
                return Ok(messages);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"❌ Error in GetMessagesByReceiver: {ex.Message}");
            }
        }
    }
}
