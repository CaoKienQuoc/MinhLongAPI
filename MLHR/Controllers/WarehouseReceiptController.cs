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

        public WarehouseReceiptController(IWarehouseReceiptService service)
        {
            _service = service;
        }

        /*[HttpPost("import-coordination/{warehouseId}")]
        public async Task<IActionResult> ImportCoordination(long warehouseId)
        {
            try
            {
                var result = await _service.CreateWarehouseReceiptFromCoordinationAsync(warehouseId);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }*/


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
    }
}
