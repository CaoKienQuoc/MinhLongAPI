using BusinessObject.DTO.Warehouse;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Services.Exceptions;
using Services.IService;

namespace MLHR.Controllers
{
    [ApiController]
    [Route("api/warehouse-receipts")]
    public class WarehouseReceiptController : ControllerBase
    {
        private readonly IWarehouseReceiptService _service;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public WarehouseReceiptController(IWarehouseReceiptService service, IHttpContextAccessor httpContextAccessor)
        {
            _service = service;
            _httpContextAccessor = httpContextAccessor;
        }

        [HttpPost("import-transfer-approved/{transferRequestId}")]
        public async Task<IActionResult> ImportApprovedTransfer(long transferRequestId)
        {
            var currentUserId = GetLoggedInUserId();
            if (currentUserId == null)
            {
                return Unauthorized(new { message = "Không xác định được người dùng đang đăng nhập." });
            }

            try
            {
                await _service.ImportApprovedTransferAsync(transferRequestId, currentUserId.Value);
                return Ok("Nhập kho điều phối thành công.");
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }


        private Guid? GetLoggedInUserId()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null || user.Identity == null || !user.Identity.IsAuthenticated)
                return null;


            // ✅ Ưu tiên lấy "UserId"
            var userIdClaim = user.Claims.FirstOrDefault(c =>
                string.Equals(c.Type, "UserId", StringComparison.OrdinalIgnoreCase) || // key tùy chỉnh
                c.Type == ClaimTypes.NameIdentifier);                                   // fallback chuẩn .NET

            if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out Guid userId))
                return userId;

            return null;
        }





        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] WarehouseReceiptRequest request)
        {
            try
            {
                // ✅ Lấy userId từ token
                var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());

                // ✅ Gọi Service để tạo phiếu nhập kho
                var result = await _service.CreateReceiptAsync(request, userId);

                if (!result)
                {
                    return BadRequest(new { success = false, message = "Lưu phiếu nhập thất bại!" });
                }

                return Ok(new { success = true, message = "Lưu phiếu nhập thành công!" });
            }
            catch (BadRequestException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống! Vui lòng thử lại sau." });
            }
        }

        /*[HttpPost("approve/{id}")]
        public async Task<IActionResult> Approve(long id)
        {
            try
            {
                var currentUserId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
                var result = await _service.ApproveReceiptAsync(id, currentUserId);
                return result ? Ok(new { success = true, message = "Approved!" }) : NotFound(new { success = false, message = "Receipt not found" });
            }
            catch (BadRequestException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống! Vui lòng thử lại sau." });
            }


        }*/

        [HttpGet("all")]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
                var receipts = await _service.GetAllReceiptsByUserAsync(userId);
                return Ok(new { success = true, data = receipts });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // ✅ Lấy chi tiết phiếu nhập theo ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
                var receipt = await _service.GetReceiptByIdAsync(id, userId);

                if (receipt == null)
                    return NotFound(new { success = false, message = "Không tìm thấy phiếu nhập hoặc bạn không có quyền truy cập." });

                return Ok(new { success = true, data = receipt });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }


        [HttpGet("print/{warehouseReceiptId}")]
        public async Task<IActionResult> PrintReceipt(long warehouseReceiptId)
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
            if (userId == null) return Unauthorized();

            try
            {
                var pdfBytes = await _service.GenerateReceiptPdfAsync(warehouseReceiptId, userId);
                return File(pdfBytes, "application/pdf", $"Phieu_Nhap_Kho_{warehouseReceiptId}.pdf");
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

            [HttpGet("dashboard/receipt-count-today")]
        public async Task<IActionResult> GetTodayReceiptCount()
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
            var count = await _service.GetTodayReceiptCountAsync(userId);
            return Ok(new { Date = DateTime.Today.ToString("yyyy-MM-dd"), ReceiptCount = count });
        }

        [HttpGet("dashboard/receipt-count-this-month")]
        public async Task<IActionResult> GetThisMonthReceiptCount()
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
            var count = await _service.GetThisMonthReceiptCountAsync(userId);
            return Ok(new { Month = DateTime.Now.Month, Year = DateTime.Now.Year, ReceiptCount = count });
        }

        [HttpGet("dashboard/receipt-total-quantity-today")]
        public async Task<IActionResult> GetTodayTotalQuantity()
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
            var quantity = await _service.GetTodayTotalQuantityAsync(userId);
            return Ok(new { Date = DateTime.Today.ToString("yyyy-MM-dd"), TotalQuantity = quantity });
        }

        [HttpGet("dashboard/receipt-total-quantity-this-month")]
        public async Task<IActionResult> GetThisMonthTotalQuantity()
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
            var quantity = await _service.GetThisMonthTotalQuantityAsync(userId);
            return Ok(new { Month = DateTime.Now.Month, Year = DateTime.Now.Year, TotalQuantity = quantity });
        }

        [HttpGet("dashboard/receipt-total-price-today")]
        public async Task<IActionResult> GetTodayTotalPrice()
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
            var totalPrice = await _service.GetTodayTotalPriceAsync(userId);
            return Ok(new { Date = DateTime.Today.ToString("yyyy-MM-dd"), TotalPrice = totalPrice });
        }

        [HttpGet("dashboard/receipt-total-price-this-month")]
        public async Task<IActionResult> GetThisMonthTotalPrice()
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
            var totalPrice = await _service.GetThisMonthTotalPriceAsync(userId);
            return Ok(new { Month = DateTime.Now.Month, Year = DateTime.Now.Year, TotalPrice = totalPrice });
        }

    }
}
