using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Services.IService;

namespace MLHR.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WarehouseExportController : ControllerBase
    {
        private readonly IWarehouseExportService _exportService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public WarehouseExportController(IWarehouseExportService exportService, IHttpContextAccessor httpContextAccessor)
        {
            _exportService = exportService;
            _httpContextAccessor = httpContextAccessor;
        }

        [HttpPost("finalize-export-sale/{exportReceiptId}")]
        public async Task<IActionResult> FinalizeExportSale(int exportReceiptId)
        {
            var userId = GetLoggedInUserId();
            if (userId == null)
            {
                return Unauthorized(new { message = "User is not authenticated" });
            }

            try
            {
                await _exportService.FinalizeExportSaleAsync(exportReceiptId, userId.Value);
                return Ok(new { success = true, message = "Phiếu xuất kho đã được xử lý thành công." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi server", detail = ex.Message });
            }
        }


        private Guid? GetLoggedInUserId()
        {
            var claimsIdentity = _httpContextAccessor.HttpContext?.User.Identity as ClaimsIdentity;
            if (claimsIdentity != null)
            {
                var userIdClaim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier); // hoặc dùng "UserId" nếu bạn lưu dưới key này
                if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out Guid userId))
                {
                    return userId;
                }
            }
            return null;
        }

    }

}
