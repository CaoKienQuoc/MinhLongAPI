using Microsoft.AspNetCore.Mvc;
using Repo.IRepository;
using Services.IService;

namespace MLHR.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AgencyScoreController : ControllerBase
    {
        private readonly IPromotionApprovalService _promotionService;

        public AgencyScoreController(IPromotionApprovalService promotionService)
        {
            _promotionService = promotionService;
        }

        [HttpPost("approve/{promotionRequestId}")]
        public async Task<IActionResult> Approve(Guid promotionRequestId)
        {
            try
            {
                await _promotionService.ApprovePromotionAsync(promotionRequestId);
                return Ok(new { message = "Duyệt thăng hạng thành công." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }

}
