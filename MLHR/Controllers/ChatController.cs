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
    }
}
