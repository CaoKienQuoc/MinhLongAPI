using Microsoft.AspNetCore.Mvc;
using static System.Net.Mime.MediaTypeNames;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using BusinessObject.Models;
using Services.IService;
using System.Security.Claims;

namespace MLHR.Controllers
{


    [ApiController]
    [Route("api/warehouse-transfer")]
    public class WarehouseTransferController : ControllerBase
    {
        private readonly IWarehouseTransferService _exportService;

        public WarehouseTransferController(IWarehouseTransferService exportService)
        {
            _exportService = exportService;
        }

        [HttpPost("approve/{id}")]
        public async Task<IActionResult> ApproveTransferRequest(int id)
        {
            try
            {
                var receipt = await _exportService.ApproveTransferRequestAndCreateReceiptAsync(id);
                return Ok(new
                {
                    success = true,
                    message = "Duyệt điều phối thành công.",
                    data = receipt
                });
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
                var data = await _exportService.GetAllTransferRequestsByUserAsync(userId);
                return Ok(new { success = true, data });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
                var result = await _exportService.GetTransferRequestByIdAsync(id, userId);
                if (result == null)
                    return NotFound(new { success = false, message = "Không tìm thấy yêu cầu điều phối." });

                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
    }
}
