using System.Security.Claims;
using BusinessObject.DTO.Warehouse;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.IService;
using Services.Service;

namespace MLHR.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class WarehouseRequestExportController : ControllerBase
    {
        private readonly IWarehouseRequestExportService _service;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public WarehouseRequestExportController(IWarehouseRequestExportService service, IHttpContextAccessor httpContextAccessor)
        {
            _service = service;
            _httpContextAccessor = httpContextAccessor;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateWarehouseRequestExport([FromQuery] int requestExportId)
        {
            var currentUserId = GetLoggedInUserId();
            if (currentUserId == null)
            {
                return Unauthorized("Không tìm thấy thông tin đăng nhập.");
            }

            var result = await _service.CreateWarehouseRequestExportAsync(requestExportId, currentUserId.Value);

            if (result == null)
                return BadRequest("Không thể tạo WarehouseRequestExport. Kiểm tra lại RequestExportId.");

            return Ok(result);
        }


        private Guid? GetLoggedInUserId()
        {
            var claimsIdentity = _httpContextAccessor.HttpContext?.User.Identity as ClaimsIdentity;
            if (claimsIdentity != null)
            {
                // 🔍 Thử các tên thường dùng trong token: "sub", "uid", "UserId"
                var userIdClaim = claimsIdentity.FindFirst("sub")
                                  ?? claimsIdentity.FindFirst("uid")
                                  ?? claimsIdentity.FindFirst("UserId");

                if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out Guid userId))
                {
                    return userId;
                }
            }

            return null;
        }

        [HttpGet("warehouse/{warehouseId}")]
        public async Task<IActionResult> GetByWarehouseId(long warehouseId, [FromQuery] string? sortBy)
        {
            var results = await _service.GetByWarehouseIdAsync(warehouseId, sortBy);
            if (results == null || !results.Any())
            {
                return NotFound("No warehouse request exports found for the given warehouse ID.");
            }

            return Ok(results);
        }

        [HttpGet("{warehouseRequestExportId}")]
        public async Task<IActionResult> GetByWarehouseRequestExportId(int warehouseRequestExportId)
        {
            try
            {
                var result = await _service.GetByWarehouseRequestExportIdAsync(warehouseRequestExportId);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error", detail = ex.Message });
            }
        }


        [HttpPost("approve")]
        public async Task<IActionResult> ApproveRequest([FromBody] ApproveWarehouseRequestExportDto requestDto)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());

                if (userId == Guid.Empty)
                    return Unauthorized(new { message = "Invalid user ID" });

                var result = await _service.ApproveRequestAsync(
                    requestDto.WarehouseRequestExportId,
                    requestDto.QuantityApproved,
                    userId
                );

                if (result)
                    return Ok(new { message = "Request approved successfully" });

                return BadRequest(new { message = "Approval failed" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        
    }
}
