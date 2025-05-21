using BusinessObject.DTO.Product;
using BusinessObject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.IService;
using Services.Service;
using System.Security.Claims;

namespace MLHR.Controllers
{
    [Route("api/request-products")]
    [ApiController]
    public class RequestProductController : ControllerBase
    {
        private readonly IRequestProductService _requestProductService;
        private readonly IUserService _userService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public RequestProductController(IRequestProductService requestProductService, IUserService userService, IHttpContextAccessor httpContextAccessor)
        {
            _requestProductService = requestProductService;
            _userService = userService;
            _httpContextAccessor = httpContextAccessor;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllRequests()
        {
            var requests = await _requestProductService.GetAllRequestsAsync();
            return Ok(requests);
        }

        //api
        // API lấy danh sách Order dựa trên AgencyId của user đang đăng nhập
        [HttpGet("my-request-product")]
        public async Task<IActionResult> GetOrdersForLoggedInAgency()
        {
            var agencyId = GetLoggedInAgencyId();
            if (agencyId == null)
            {
                return Unauthorized(new { message = "User is not associated with any agency" });
            }

            var orders = await _requestProductService.GetRequestProductsByAgencyIdAsync(agencyId.Value);

            if (orders == null || !orders.Any())
            {
                return NotFound(new { message = "No orders found for this agency" });
            }

            return Ok(orders);
        }

        private long? GetLoggedInAgencyId()
        {
            var claimsIdentity = _httpContextAccessor.HttpContext?.User.Identity as ClaimsIdentity;
            if (claimsIdentity != null)
            {
                var agencyIdClaim = claimsIdentity.FindFirst("AgencyId");
                if (agencyIdClaim != null && long.TryParse(agencyIdClaim.Value, out long agencyId))
                {
                    return agencyId;
                }
            }
            return null;
        }

        [HttpGet("{requestId}")]
        public async Task<IActionResult> GetRequestProductById(Guid requestId)
        {
            try
            {
                var requestProduct = await _requestProductService.GetRequestByIdAsync(requestId);
                return Ok(requestProduct);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }
        [HttpPost]
        [Authorize(Roles = "2")]
        public async Task<IActionResult> CreateRequest([FromBody] CreateRequestProductDto requestDto)
        {
            try
            {
                if (requestDto == null || requestDto.Products == null || requestDto.Products.Count == 0)
                {
                    return BadRequest("Request must contain at least one product.");
                }

                // **Lấy UserId từ token dưới dạng GUID**
                var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

                // **Gọi Service để lấy AgencyId từ UserId**
                var agencyId = await _userService.GetAgencyIdByUserId(userId);
                if (!agencyId.HasValue)
                {
                    return BadRequest("User does not belong to any agency.");
                }

                var requestProduct = new RequestProduct
                {
                    AgencyId = agencyId.Value, // ✅ Chuyển `long?` thành `long`
                    CreatedAt = DateTime.UtcNow
                };

                var requestDetails = new List<RequestProductDetail>();
                foreach (var item in requestDto.Products)
                {
                    requestDetails.Add(new RequestProductDetail
                    {
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        Unit = item.Unit
                    });
                }

                await _requestProductService.CreateRequestAsync(requestProduct, requestDetails, userId);
                return Ok(requestProduct);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message }); // ✅ Sử dụng BadRequest() ở Controller
            }
            /*catch (Exception ex)
            {
                // Nếu là lỗi khác, thay vì 500, vẫn trả về lỗi 400 BadRequest với nội dung tùy chỉnh
                return StatusCode(500, new { error = "Đã có lỗi xảy ra trong hệ thống." });
            }*/
        }


        [HttpPut("{requestId}/cancel")]
        public async Task<IActionResult> CancelRequest(Guid requestId)
        {
            try
            {
                // **Lấy UserId từ Token**
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                {
                    return Unauthorized(new { error = "User is not authenticated." });
                }

                var userId = Guid.Parse(userIdClaim.Value);

                // **Lấy Role của User từ Token**
                var roleClaim = User.FindFirst(ClaimTypes.Role);
                if (roleClaim == null)
                {
                    return Forbid("User role not found.");
                }

                var userRole = roleClaim.Value;

                // **Kiểm tra nếu Role khác "4" thì từ chối quyền**
                if (userRole != "4")
                {
                    return StatusCode(403, "Only SALES MANAGEMENT (Role 4) can approve requests."); // 🚀 Cách đúng để trả về 403
                }

                // **Gọi Service để lấy EmployeeId từ UserId**
                var employeeId = await _userService.GetEmployeeIdByUserId(userId);
                if (!employeeId.HasValue)
                {
                    return BadRequest("User does not belong to any employee.");
                }
                if (userIdClaim == null)
                {
                    return Unauthorized(new { error = "User is not authenticated." });
                }
                var result = await _requestProductService.CancelRequestAsync(requestId, employeeId.Value);
                if (!result)
                    return BadRequest(new { message = "Failed to cancel request." });

                return Ok(new { message = "Request canceled successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

    }
}
