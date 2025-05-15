using System.Security.Claims;
using BusinessObject.DTO.AgencyLevel;
using BusinessObject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.IService;

namespace MLHR.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    
    public class AgencyLevelController : ControllerBase
    {
        private readonly IAgencyLevelService _service;

        public AgencyLevelController(IAgencyLevelService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var levels = await _service.GetAllLevelsAsync();
            return Ok(levels);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            var level = await _service.GetLevelByIdAsync(id);
            if (level == null) return NotFound();
            return Ok(level);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateAgencyLevelDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            await _service.CreateLevelAsync(dto);
            return Ok();
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateAgencyLevelDto dto)
        {
            try
            {
                await _service.UpdateLevelAsync(id, dto);
                return Ok(new { message = "Cập nhật cấp đại lý thành công." });
            }
            catch (Exception ex)
            {
                // Có thể phân loại lỗi chi tiết hơn nếu muốn
                return BadRequest(new { message = ex.Message });
            }
        }



        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            await _service.DeleteLevelAsync(id);
            return NoContent();
        }

        [HttpGet("current")]
        public async Task<IActionResult> GetCurrentLevelOfAgency()
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());

            if (userId == Guid.Empty)
                return Unauthorized();

            var result = await _service.GetCurrentLevelByUserIdAsync(userId);

            if (result == null)
                return NotFound("Không tìm thấy cấp đại lý hiện tại");

            return Ok(result);
        }


        [HttpGet("dashboard/count-by-level")]
        public async Task<IActionResult> GetAgencyCountByLevel()
        {
            var result = await _service.GetAgencyCountByLevelAsync();
            return Ok(result);
        }


    }
}
