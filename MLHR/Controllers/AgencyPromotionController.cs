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
        private readonly IUserRepository _employeeRepo;

        public AgencyScoreController(IPromotionApprovalService promotionService, IHttpContextAccessor httpContextAccessor, IUserRepository userRepository)
        {
            _promotionService = promotionService;
            _httpContextAccessor = httpContextAccessor;
            _employeeRepo = userRepository;
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

        [HttpGet("managed")]
        public async Task<IActionResult> GetManagedRequests()
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
            var employee = await _employeeRepo.GetByUserIdAsync(userId);
            if (employee == null)
                return Unauthorized(new { message = "Không tìm thấy thông tin nhân viên." });

            var result = await _promotionService.GetRequestsManagedByEmployeeIdAsync(employee.EmployeeId);
            return Ok(result);
        }

        [HttpGet("managed/{id}")]
        public async Task<IActionResult> GetManagedRequestById(Guid id)
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
            var employee = await _employeeRepo.GetByUserIdAsync(userId);
            if (employee == null)
                return Unauthorized(new { message = "Không tìm thấy thông tin nhân viên." });

            var request = await _promotionService.GetRequestByIdManagedAsync(id, employee.EmployeeId);
            if (request == null)
                return NotFound(new { message = "Không tìm thấy yêu cầu hoặc bạn không có quyền." });

            return Ok(request);
        }

        [HttpGet("get-my-score")]
        public async Task<IActionResult> GetMyScoreDetail()
        {
            try
            {
                var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
                var dto = await _promotionService.GetScoreDetailByUserIdAsync(userId);
                return Ok(dto);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }

}
