using Microsoft.AspNetCore.Mvc;
using static System.Net.Mime.MediaTypeNames;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using BusinessObject.Models;
using Services.IService;

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
    }

}
