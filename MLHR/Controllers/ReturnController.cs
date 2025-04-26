using BusinessObject.DTO.ReturnOrder;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Services.IService;
using Services.Service;
using BusinessObject.Models;

namespace MLHR.Controllers
{
    [ApiController]
    [Route("api/returns")]
    public class ReturnController : ControllerBase
    {
        private readonly IReturnService _returnService;
        private readonly IDamagedStockService _damagedStockService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ReturnController(IReturnService returnService, IHttpContextAccessor httpContextAccessor, IDamagedStockService damagedStock)
        {
            _returnService = returnService;
            _httpContextAccessor = httpContextAccessor;
            _damagedStockService = damagedStock;
        }

        private Guid? GetLoggedInUserId()
        {
            var claimsIdentity = _httpContextAccessor.HttpContext?.User.Identity as ClaimsIdentity;
            if (claimsIdentity != null)
            {
                var userIdClaim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier); // hoặc "UserId" tùy theo setup JWT
                if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out Guid userId))
                {
                    return userId;
                }
            }
            return null;
        }


        [HttpPost("create")]
        [Authorize]
        [RequestSizeLimit(10_000_000)]
        public async Task<IActionResult> CreateReturnRequestWithImages(
        [FromForm] Guid orderId,
        [FromForm] string? note,
        [FromForm] Guid orderDetailId,
        [FromForm] int quantity,
        [FromForm] string reason,
        [FromForm] List<IFormFile> images)
        {
            var userId = GetLoggedInUserId();
            if (userId == null) return Unauthorized();

            var result = await _returnService.CreateReturnRequestWithImagesAsync(orderId, orderDetailId, quantity, reason, note, userId.Value, images);
            return Ok(new { message = "Tạo yêu cầu trả hàng thành công", data = result });
        }




        [HttpPut("approve/{returnRequestId}")]
        public async Task<IActionResult> ApproveReturnRequest(Guid returnRequestId)
        {
            try
            {
                await _returnService.ApproveReturnRequestAsync(returnRequestId);
                return Ok(new { message = "Đã duyệt yêu cầu trả hàng." });
            }
            catch (Exception ex)
            {
                var deepestMessage = ex;
                while (deepestMessage.InnerException != null)
                    deepestMessage = deepestMessage.InnerException;

                return BadRequest(new { message = $"Duyệt yêu cầu trả hàng thất bại: {deepestMessage.Message}" });
            }
        }



        // 🟢 3. Kho xác nhận nhập hàng trả
        [HttpPost("{warehouseReceiptId:long}/cancel")]
        public async Task<IActionResult> CancelAndImport(long warehouseReceiptId)
        {
            try
            {
                var userId = GetCurrentUserId();
                await _damagedStockService.ImportToDamagedStockAsync(warehouseReceiptId, userId);
                return Ok(new { success = true, message = "Đã import vào kho huỷ." });
            }
            catch (Exception ex)
            {
                // Chỉ show message lỗi
                return BadRequest(new { success = false, message = ex.Message });
            }
        }



        // 🟠 4. Xem danh sách yêu cầu trả hàng đang chờ duyệt
        [HttpGet("pending")]
        public async Task<IActionResult> GetPendingReturns()
        {
            var result = await _returnService.GetPendingReturnsAsync();
            return Ok(result);
        }

        // ✅ Utility để lấy userId từ token
        private Guid GetCurrentUserId()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(userIdStr, out var userId)
                ? userId
                : throw new UnauthorizedAccessException("Không xác định được người dùng.");
        }

        // GET api/damagedstock/warehouse/5
        [HttpGet("warehouse/{warehouseId:long}")]
        public async Task<IActionResult> GetByWarehouseId(long warehouseId)
        {
            var list = await _damagedStockService.GetByWarehouseIdAsync(warehouseId);
            if (!list.Any())
            {
                return NotFound(new
                {
                    success = false,
                    message = $"Không tìm thấy bản ghi nào cho kho {warehouseId}."
                });
            }

            return Ok(new
            {
                success = true,
                message = $"Đã tìm thấy {list.Count()} bản ghi.",
                data = list
            });
        }
        // 🟢 5. Lấy danh sách tất cả yêu cầu trả hàng
        [HttpGet]
        public async Task<IActionResult> GetAllReturnRequests()
        {
            var result = await _returnService.GetAllReturnRequestsAsync();
            return Ok(result);
        }

        // 🟢 6. Lấy chi tiết yêu cầu trả hàng theo ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetReturnRequestById(Guid id)
        {
            var result = await _returnService.GetReturnRequestByIdAsync(id);
            if (result == null)
                return NotFound(new { message = "Không tìm thấy yêu cầu trả hàng." });

            return Ok(result);
        }


        // 🟢 7. Lấy danh sách yêu cầu trả hàng đã duyệt
        [HttpGet("for-warehouse")]
        [Authorize]
        public async Task<IActionResult> GetApprovedReturnRequests()
        {
            var result = await _returnService.GetApprovedReturnRequestsAsync();
            return Ok(result);
        }

        // 🟢 8. Lấy chi tiết yêu cầu trả hàng đã duyệt theo ID
        [HttpGet("for-warehouse/{id}")]
        [Authorize]
        public async Task<IActionResult> GetApprovedReturnRequestById(Guid id)
        {
            var result = await _returnService.GetApprovedReturnRequestByIdAsync(id);
            if (result == null)
                return NotFound(new { message = "Không tìm thấy yêu cầu trả hàng đã duyệt." });

            return Ok(result);
        }

        [HttpGet("return-receipts")]
        public async Task<IActionResult> GetAllReturnWarehouseReceipts()
        {
            var result = await _returnService.GetAllReturnWarehouseReceiptsAsync();
            return Ok(result);
        }

        // 🟢 2. Lấy chi tiết phiếu nhập trả hàng theo ID
        [HttpGet("return-receipts/{id}")]
        public async Task<IActionResult> GetReturnWarehouseReceiptById(long id)
        {
            var result = await _returnService.GetReturnWarehouseReceiptByIdAsync(id);
            if (result == null)
                return NotFound(new { message = "Không tìm thấy phiếu nhập trả hàng." });

            return Ok(result);
        }

    }
}
