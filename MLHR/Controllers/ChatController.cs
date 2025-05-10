using BusinessObject.DTO;
using BusinessObject.Models;
using Microsoft.AspNetCore.Mvc;
using Services.IService;

namespace MLHR.Controllers
{
    // ChatController.cs
    [ApiController]
    [Route("api/chat")]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;
        private readonly ILogger<ChatController> _logger;
        public ChatController(IChatService chatService, ILogger<ChatController> logger)
        {
            _chatService = chatService;
            _logger = logger;
        }

        [HttpPost("rooms")]
        public async Task<IActionResult> CreateRoom([FromBody] CreateRoomDto dto)
        {
            if (dto == null || dto.MemberIds == null || !dto.MemberIds.Any())
                return BadRequest(new { error = "Bạn Chưa Đăng Nhập!" });

            try
            {
                var room = await _chatService.CreateRoomAsync(dto.RoomName, dto.MemberIds);
                return CreatedAtAction(nameof(GetRoom), new { roomId = room.ChatRoomId }, room);
            }
            catch (Exception ex)
            {
                // Ghi log stack trace
                _logger.LogError(ex, "CreateRoom failed for RoomName={RoomName}", dto.RoomName);
                // Trả về JSON lỗi
                return StatusCode(500, new { error = ex.Message });
            }
        }


        [HttpGet("rooms")]
        public async Task<IActionResult> GetMyRooms()
        {
            var claim = User.FindFirst("UserId");
            if (claim == null)
                return Unauthorized(new { error = "Bạn Chưa Đăng Nhập!" });

            if (!Guid.TryParse(claim.Value, out var userId))
                return BadRequest(new { error = "Bạn Chưa Đăng Nhập!" });

            var rooms = await _chatService.GetUserRoomsAsync(userId);
            return Ok(rooms);
        }


        // GET /api/chat/rooms/{roomId}
        [HttpGet("rooms/{roomId:guid}")]
        public async Task<IActionResult> GetRoom(Guid roomId)
        {
            // 1) Kiểm tra claim UserId (đảm bảo đã authentication)
            var claim = User.FindFirst("UserId");
            if (claim == null)
                return Unauthorized(new { error = "Bạn Chưa Đăng Nhập!" });

            if (!Guid.TryParse(claim.Value, out var userId))
                return BadRequest(new { error = "Bạn Chưa Đăng Nhập!" });

            try
            {
                // 2) Lấy DTO
                var roomDto = await _chatService.GetRoomByIdAsync(roomId);

                // 3) Nếu không tìm thấy hoặc user không thuộc room thì trả về 404
                if (roomDto == null || !roomDto.MemberIds.Contains(userId))
                    return NotFound(new { error = "Chat room not found or access denied" });

                return Ok(roomDto);
            }
            catch (Exception ex)
            {
                // 4) Bắt mọi exception bất ngờ, log nếu cần
                // _logger.LogError(ex, "Error in GetRoom {RoomId}", roomId);
                return StatusCode(500, new { error = ex.Message });
            }
        }


        // GET /api/chat/rooms/{roomId}/messages
        [HttpGet("rooms/{roomId:guid}/messages")]
        public async Task<IActionResult> GetMessages(Guid roomId, int skip = 0, int take = 200)
        {
            // 1) Lấy userId từ JWT
            var claim = User.FindFirst("UserId");
            if (claim == null || !Guid.TryParse(claim.Value, out var userId))
                return Unauthorized(new { error = "Bạn Chưa Đăng Nhập!" });

            try
            {
                // 2) Kiểm tra user có trong room không
                var isMember = await _chatService.IsUserInRoomAsync(roomId, userId);
                if (!isMember)
                    return Forbid();  // hoặc NotFound tuỳ chính sách

                // 3) Lấy messages
                var dtos = await _chatService.GetMessagesAsync(roomId, skip, take);
                return Ok(dtos);
            }
            catch (Exception ex)
            {
                // 4) Bắt và trả lỗi 500
                return StatusCode(500, new { error = ex.Message });
            }
        }

    }



}
