using BusinessObject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.IService;

namespace MLHR.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Bắt buộc đăng nhập
    public class DamagedStockController : ControllerBase
    {
        private readonly IDamagedStockService _damagedStockService;

        public DamagedStockController(IDamagedStockService damagedStockService)
        {
            _damagedStockService = damagedStockService;
        }

        [HttpGet("warehouse")]
        public async Task<IActionResult> GetDamagedStockByUserWarehouseAsync()
        {
            try
            {
                // ✅ Lấy UserId từ token
                var userIdClaim = User.FindFirst("userId")?.Value;
                if (userIdClaim == null)
                    return Unauthorized("Không tìm thấy thông tin người dùng.");

                var userId = Guid.Parse(userIdClaim);
                var damagedStocks = await _damagedStockService.GetByUserWarehouseAsync(userId);

                return Ok(damagedStocks);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi hệ thống: {ex.Message}");
            }
        }

        [HttpGet("damaged-total")]
        public async Task<IActionResult> GetDamagedStockTotalAsync([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            try
            {
                var result = await _damagedStockService.GetTotalByStatusAndDateAsync(startDate, endDate);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi hệ thống: {ex.Message}");
            }
        }

    }
}
