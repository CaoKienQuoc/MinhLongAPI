using Microsoft.AspNetCore.Mvc;
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

        [HttpPost("import-coordination/{warehouseId}")]
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
        }
    }
}
