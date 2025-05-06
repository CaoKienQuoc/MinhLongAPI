using Microsoft.AspNetCore.Mvc;
using Repo.IRepository;
using Services.IService;
using System.Security.Claims;

namespace MLHR.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AgencyScoreController : ControllerBase
    {
        private readonly IPromotionApprovalService _promotionService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AgencyScoreController(IPromotionApprovalService promotionService, IHttpContextAccessor httpContextAccessor)
        {
            _promotionService = promotionService;
            _httpContextAccessor = httpContextAccessor;
        }

        [HttpPost("approve/{promotionRequestId}")]
        public async Task<IActionResult> Approve(Guid promotionRequestId)
        {
            try
            {
                var userId = GetLoggedInUserId();
                if (userId == null)
                    return Unauthorized(new { message = "Bạn chưa đăng nhập." });
                await _promotionService.ApprovePromotionAsync(promotionRequestId, userId);
                return Ok(new { message = "Duyệt thăng hạng thành công." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        private Guid GetLoggedInUserId()
        {
            var claimsIdentity = _httpContextAccessor.HttpContext?.User.Identity as ClaimsIdentity;
            if (claimsIdentity != null)
            {
                var userIdClaim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier); // "sub" hoặc "UserId" nếu JWT khác
                if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out Guid userId))
                {
                    return userId;
                }
            }

            throw new UnauthorizedAccessException("Không xác định được người dùng đăng nhập.");
        }

    }

}
