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

        [HttpGet("all")]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
                var exports = await _exportService.GetAllExportsByUserAsync(userId);
                return Ok(new { success = true, data = exports });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
                var export = await _exportService.GetExportByIdAsync(id, userId);
                if (export == null)
                    return NotFound(new { success = false, message = "Không tìm thấy phiếu xuất hoặc bạn không có quyền truy cập." });

                return Ok(new { success = true, data = export });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
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


        [HttpGet("print/{exportReceiptId}")]
        public async Task<IActionResult> PrintExportReceipt(int exportReceiptId)
        {
            var userId = GetLoggedInUserId();
            if (userId == null) return Unauthorized();

            try
            {
                var pdfBytes = await _exportService.GenerateExportReceiptPdfAsync(exportReceiptId, userId.Value);
                return File(pdfBytes, "application/pdf", $"Phieu_Xuat_Kho_{exportReceiptId}.pdf");
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }

        }

            [HttpGet("dashboard/export-count-today")]
        public async Task<IActionResult> GetExportCountToday()
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
            var count = await _exportService.GetTodayExportCountAsync(userId);
            return Ok(new { Date = DateTime.Today, ExportCount = count });
        }

        [HttpGet("dashboard/export-count-this-month")]
        public async Task<IActionResult> GetExportCountThisMonth()
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
            var count = await _exportService.GetThisMonthExportCountAsync(userId);
            return Ok(new { Month = DateTime.Now.Month, Year = DateTime.Now.Year, ExportCount = count });
        }

        [HttpGet("dashboard/export-total-quantity-today")]
        public async Task<IActionResult> GetExportQuantityToday()
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
            var qty = await _exportService.GetTodayExportQuantityAsync(userId);
            return Ok(new { Date = DateTime.Today, TotalQuantity = qty });
        }

        [HttpGet("dashboard/export-total-quantity-this-month")]
        public async Task<IActionResult> GetExportQuantityThisMonth()
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
            var qty = await _exportService.GetThisMonthExportQuantityAsync(userId);
            return Ok(new { Month = DateTime.Now.Month, Year = DateTime.Now.Year, TotalQuantity = qty });
        }

        [HttpGet("dashboard/export-total-price-today")]
        public async Task<IActionResult> GetExportValueToday()
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
            var value = await _exportService.GetTodayExportValueAsync(userId);
            return Ok(new { Date = DateTime.Today, TotalAmount = value });
        }

        [HttpGet("dashboard/export-total-price-this-month")]
        public async Task<IActionResult> GetExportValueThisMonth()
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
            var value = await _exportService.GetThisMonthExportValueAsync(userId);
            return Ok(new { Month = DateTime.Now.Month, Year = DateTime.Now.Year, TotalAmount = value });
        }

        [HttpGet("dashboard/monthly-export-stats")]
        public async Task<IActionResult> GetMonthlyExportStats()
        {
            try
            {
                var stats = await _exportService.GetMonthlyExportStatsAllAsync();
                return Ok(new { success = true, data = stats });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("dashboard/export-summary")]
        public async Task<IActionResult> GetExportDashboard([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
        {
            try
            {
                var dashboardData = await _exportService.GetExportDashboardAsync(fromDate, toDate);
                return Ok(new { success = true, data = dashboardData });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("dashboard/profit")]
        public async Task<IActionResult> GetProfitStats([FromQuery] int? year, [FromQuery] int? month)
        {
            try
            {
                var profitStats = await _exportService.GetProfitStatsAsync(year, month);
                return Ok(new { success = true, data = profitStats });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("dashboard/profit-year")]
        public async Task<IActionResult> GetAnnualProfit([FromQuery] int? year)
        {
            try
            {
                var result = await _exportService.GetAnnualProfitAsync(year);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }



    }

}
