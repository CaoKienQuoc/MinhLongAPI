using BusinessObject.DTO;
using BusinessObject.DTO.Warehouse;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Services.IService;
using SkiaSharp;

namespace MLHR.Controllers
{
    [Route("api/batch")]
    [ApiController]
    [Authorize(Roles = "3, 4")]
    public class BatchController : ControllerBase
    {
        private readonly IBatchService _batchService;

        public BatchController(IBatchService batchService)
        {
            _batchService = batchService;
        }

        [HttpPut("update-profit-margin/{batchId}/{profitMarginPercent}")]
        public async Task<IActionResult> UpdateProfitMargin(long batchId, decimal profitMarginPercent)
        {
            var result = await _batchService.UpdateProfitMarginAsync(batchId, profitMarginPercent);

            if (!result.Success)
                return BadRequest(result.Message);

            return Ok(result.Data);
        }

        [HttpGet("by-product/{productId}")]
        public async Task<IActionResult> GetBatchesByProduct(long productId)
        {
            var batches = await _batchService.GetBatchesByProductIdAsync(productId);

            if (batches == null || !batches.Any())
                return NotFound("No batches found for the given product.");

            return Ok(batches);
        }

        [HttpGet("by-batch/{batchId}")]
        public async Task<IActionResult> GetProductInfoByBatchId(long batchId)
        {
            var result = await _batchService.GetProductInfoByBatchIdAsync(batchId);
            if (result == null)
                return NotFound("Batch not found");

            return Ok(result);
        }

        [HttpGet("batches/warehouse/{warehouseId}")]
        public async Task<IActionResult> GetBatchesByWarehouse(long warehouseId)
        {
            try
            {
                var batches = await _batchService.GetBatchesByWarehouseIdAsync(warehouseId);
                if (!batches.Any())
                    return NotFound("Không tìm thấy batch nào cho kho này.");

                return Ok(batches);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("cancel-expired/{batchId}")]
        public async Task<IActionResult> CancelExpiredBatch(long batchId)
        {
            var result = await _batchService.CancelExpiredBatchAsync(batchId);
            if (!result)
                return BadRequest("Batch không tồn tại hoặc không phải trạng thái EXPIRED.");

            return Ok("Nhập huỷ thành công.");
        }

        [HttpPut("batches/{batchId}")]
        public async Task<IActionResult> UpdateBatch(
        long batchId,
        [FromBody] UpdateBatchDto dto)
        {

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Lấy userId từ JWT nếu cần track
            var claim = User.FindFirst("UserId");
            if (claim == null || !Guid.TryParse(claim.Value, out var userId))
                return Unauthorized();

            try
            {
                var updated = await _batchService.UpdateBatchAsync(dto, userId, batchId);
                return Ok(updated);
            }
            catch (KeyNotFoundException knf)
            {
                return NotFound(knf.Message);
            }
            catch (ArgumentException ae)
            {
                return BadRequest(ae.Message);
            }
            catch (InvalidOperationException ioe)
            {
                return Conflict(ioe.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}
