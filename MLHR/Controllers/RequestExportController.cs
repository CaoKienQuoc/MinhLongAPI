using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.IService;

namespace MLHR.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RequestExportController : ControllerBase
    {
        private readonly IRequestExportService _requestExportService;
        private readonly IWarehouseExportService _warehouseExportService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public RequestExportController(
            IRequestExportService requestExportService, 
            IWarehouseExportService warehouseExportService, 
            IHttpContextAccessor httpContextAccessor)
        {
            _requestExportService = requestExportService;
            _warehouseExportService = warehouseExportService;
            _httpContextAccessor = httpContextAccessor;
        }

        // ✅ API GET: Lấy danh sách RequestExport kèm RequestExportDetail
        [HttpGet("all")]
        //[Authorize(Roles = "3, 4")]
        public async Task<IActionResult> GetAllRequestExports([FromQuery] string? sortBy)
        {
            var requestExports = await _requestExportService.GetAllRequestExportsAsync(sortBy);
            return Ok(requestExports);
        }

        [HttpGet("{requestExportId}")]
        //[Authorize(Roles = "3, 4")]
        public async Task<IActionResult> GetRequestExportByID(int requestExportId)
        {
            var requestExports = await _requestExportService.GetRequestExportByIdAsync(requestExportId);
            return Ok(requestExports);
        }

        [HttpPost("create-for-main-warehouse/{requestExportId}")]
        public async Task<IActionResult> CreateForMainWarehouse(int requestExportId)
        {
            try
            {
                var currentUserId = GetLoggedInUserId();

                var result = await _warehouseExportService.CreateExportReceiptForMainWarehouseAsync(requestExportId, currentUserId);

                return Ok(new
                {
                    success = true,
                    message = "Tạo phiếu xuất kho chính thành công.",
                    data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        private Guid GetLoggedInUserId()
        {
            var claimsIdentity = _httpContextAccessor.HttpContext?.User.Identity as ClaimsIdentity;
            if (claimsIdentity != null)
            {
                var userIdClaim = claimsIdentity.FindFirst("UserId"); // 👉 đổi nếu bạn dùng "sub" hay "id"
                if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out Guid userId))
                {
                    return userId;
                }
            }

            throw new UnauthorizedAccessException("Không tìm thấy thông tin người dùng đăng nhập.");
        }
    }
}
