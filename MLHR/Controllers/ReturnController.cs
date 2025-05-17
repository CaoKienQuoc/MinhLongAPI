using BusinessObject.DTO.ReturnOrder;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Services.IService;
using Services.Service;
using BusinessObject.Models;
using Newtonsoft.Json;

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
        [RequestSizeLimit(20_000_000)]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CreateReturnRequestWithImages([FromForm] ReturnRequestFormDto dto)
        {
            var userId = GetLoggedInUserId();
            if (userId == null)
                return Unauthorized(new { message = "Người dùng chưa đăng nhập" });

            try
            {
                Console.WriteLine("ItemsJson nhận vào:");
                Console.WriteLine(dto.ItemsJson);

                var itemDetails = JsonConvert
                    .DeserializeObject<List<FlattenedReturnItemDto>>(dto.ItemsJson) ?? new();

                await _returnService.CreateReturnRequestWithImagesAsync(
                    dto.OrderId,
                    itemDetails.Select(i => (i.OrderDetailId, i.Quantity, i.Reason)).ToList(),
                    userId.Value,
                    dto.Images
                );

                return Ok(new { message = "Tạo yêu cầu trả hàng thành công" });
            }
            catch (JsonException jsonEx)
            {
                // Lỗi khi parse JSON sai định dạng
                return BadRequest(new { message = "Dữ liệu ItemsJson không hợp lệ", detail = jsonEx.Message });
            }
            catch (Exception ex)
            {
                // Lỗi không xác định
                return StatusCode(500, new { message = "Có lỗi xảy ra khi tạo yêu cầu trả hàng", detail = ex.Message });
            }
        }




        [HttpPut("approve-Return-Request/{returnRequestId}")]
        public async Task<IActionResult> ApproveReturnRequest(Guid returnRequestId)
        {
            try
            {
                var userId = GetCurrentUserId();
                await _returnService.ApproveReturnRequestAsync(returnRequestId, userId);
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

        [HttpPost("reject-Return-Request/{returnRequestId}")]
        public async Task<IActionResult> Reject(Guid returnRequestId, [FromBody] RejectReturnRequestDto dto)
        {
            try
            {
                var userId = GetCurrentUserId();
                await _returnService.RejectReturnRequestAsync(returnRequestId, userId, dto.Reason);
                return Ok(new { message = "Từ chối yêu cầu trả hàng thành công." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }



        // 🟢 3. Kho xác nhận nhập hàng trả
        [HttpPost("{warehouseReceiptId:long}/Import-Damage-Stock")]
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
        [HttpGet("{returnRequestId}")]
        public async Task<IActionResult> GetReturnRequestById(Guid returnRequestId)
        {
            var result = await _returnService.GetReturnRequestByIdAsync(returnRequestId);
            if (result == null)
                return NotFound(new { message = "Không tìm thấy yêu cầu trả hàng." });

            return Ok(result);
        }


        [HttpGet("return-requests/sale")]
        public async Task<IActionResult> GetAllReturnRequestsForSales()
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());

            var result = await _returnService.GetAllReturnRequestsAsyncForSales(userId);
            return Ok(result);
        }


        [HttpGet("return-requests/sale/{id}")]
        public async Task<IActionResult> GetReturnRequestByIdForSales(Guid id)
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());

            try
            {
                var result = await _returnService.GetReturnRequestByIdAsyncForSales(id, userId);
                if (result == null)
                    return NotFound("Không tìm thấy yêu cầu trả hàng.");

                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
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
        [HttpGet("for-warehouse/{returnRequestId}")]
        [Authorize]
        public async Task<IActionResult> GetApprovedReturnRequestById(Guid returnRequestId)
        {
            var result = await _returnService.GetApprovedReturnRequestByIdAsync(returnRequestId);
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
        [HttpGet("return-receipts/{ReturnWarehouseReceiptId}")]
        public async Task<IActionResult> GetReturnWarehouseReceiptById(long ReturnWarehouseReceiptId)
        {
            var result = await _returnService.GetReturnWarehouseReceiptByIdAsync(ReturnWarehouseReceiptId);
            if (result == null)
                return NotFound(new { message = "Không tìm thấy phiếu nhập trả hàng." });

            return Ok(result);
        }

        [HttpGet("warehouse-return/WarehouseReturnByWarehouseId/{warehouseId}")]
        public async Task<IActionResult> GetByWarehouseIdInReturnWarhouse(long warehouseId)
        {
            var receipts = await _returnService.GetByWarehouseIdAsync(warehouseId);

            return Ok(new
            {
                message = "Danh sách phiếu nhập trả kho",
                data = receipts ?? new List<ReturnWarehouseReceiptDto>() // ✅ Nếu receipts null thì trả [] luôn
            });
        }
       
        [HttpGet("agency-return")]
        [Authorize] // yêu cầu login
        public async Task<IActionResult> GetReturnRequests()
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
            if (userId == Guid.Empty)
                return Unauthorized();

            var requests = await _returnService.GetReturnRequestsByUserIdAsync(userId);
            return Ok(requests);
        }

        // GET: api/return-requests/{id}
        [HttpGet("agency-return/{id}")]
        [Authorize]
        public async Task<IActionResult> GetReturnRequestByIdForAgency(Guid id)
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
            if (userId == Guid.Empty)
                return Unauthorized();

            var request = await _returnService.GetReturnRequestByIdAsync(id, userId);
            if (request == null)
                return NotFound("Return request not found.");

            return Ok(request);
        }

    }
}
