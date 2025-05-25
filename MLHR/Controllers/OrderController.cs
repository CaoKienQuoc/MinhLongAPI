using BusinessObject.DTO.Order;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.IService;
using Services.Service;
using System.Security.Claims;

namespace MLHR.Controllers
{
    [Route("api/orders")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly IHttpContextAccessor _httpContextAccessor;


        public OrderController(IOrderService orderService, IHttpContextAccessor httpContextAccessor)
        {
            _orderService = orderService;
            _httpContextAccessor = httpContextAccessor;
        }

        // ✅ API để lấy danh sách Order (Bao gồm chi tiết đơn hàng)
        [HttpGet]
        public async Task<IActionResult> GetAllOrders()
        {
            var orders = await _orderService.GetAllOrdersAsync();
            /*return Ok(orders.Select(o => new
            {
                OrderId = o.OrderId,
                orderCode = o.OrderCode,
                OrderDate = o.OrderDate,
                TotalAmount = o.OrderDetails.Sum(od => od.UnitPrice * od.Quantity), // Tính tổng tiền
                Status = o.Status,
                OrderDetails = o.OrderDetails.Select(od => new
                {
                    ProductId = od.ProductId,
                    Quantity = od.Quantity,
                    Price = od.UnitPrice,
                    SubTotal = od.Quantity * od.UnitPrice
                })
            }));*/

            return Ok(orders);
        }

        // API để lấy chi tiết một Order
        [HttpGet("{orderId}")]
        public async Task<IActionResult> GetOrderById(Guid orderId)
        {
            var order = await _orderService.GetOrderByIdAsync(orderId);
            if (order == null) return NotFound("Không tìm thấy đơn hàng!");
            /*return Ok(new
            {
                OrderId = order.OrderId,
                OrderDate = order.OrderDate,
                TotalAmount = order.OrderDetails.Sum(od => od.UnitPrice * od.Quantity), // Tính tổng tiền
                Status = order.Status,
                OrderDetails = order.OrderDetails.Select(od => new
                {
                    ProductId = od.ProductId,
                    Quantity = od.Quantity,
                    Price = od.UnitPrice,
                    SubTotal = od.Quantity * od.UnitPrice
                })
            });*/

            return Ok(order);
        }


        [HttpGet("manage-by-sales")]
        public async Task<IActionResult> GetOrdersBySales()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out Guid userId))
                return Unauthorized("Invalid user token.");

            var orders = await _orderService.GetAllOrdersBySalesUserAsync(userId);
            return Ok(orders);
        }

        [HttpGet("manage-by-sales/{orderId}")]
        public async Task<IActionResult> GetOrderDetailBySales(Guid orderId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out Guid userId))
                return Unauthorized("Invalid user token.");

            var order = await _orderService.GetOrderByIdBySalesUserAsync(orderId, userId);
            return Ok(order);
        }


        [HttpGet("my-orders")]
        public async Task<IActionResult> GetOrdersForLoggedInAgency()
        {
            var agencyId = GetLoggedInAgencyId();
            if (agencyId == null)
            {
                return Unauthorized(new { message = "User is not associated with any agency" });
            }

            // ✅ Trả về mảng rỗng nếu không có đơn
            var orders = await _orderService.GetOrdersByAgencyIdAsync(agencyId.Value) ?? new List<OrderDto>();

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

        // API thanh toán đơn hàng
        [HttpPut("{orderId}/payment")]
        [Authorize(Roles = "2")]
        public async Task<IActionResult> ProcessPayment(Guid orderId)
        {
            try
            {
                var result = await _orderService.ProcessPaymentAsync(orderId);
                if (result)
                    return Ok(new { message = "Payment processed successfully!" });

                return BadRequest(new { message = "Failed to process payment." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{orderId}/cancel")]
        public async Task<IActionResult> CancelRequest(Guid orderId)
        {
            try
            {
                var result = await _orderService.CancelOrderAsync(orderId);
                if (!result)
                    return BadRequest(new { message = "Failed to cancel request." });

                return Ok(new { message = "Order canceled successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }


        [HttpGet("dashboard/order-status-count")]
        public async Task<IActionResult> GetOrderStatusCounts()
     => Ok(await _orderService.GetOrderStatusCountsAsync());

        [HttpGet("dashboard/daily-revenue")]
        public async Task<IActionResult> GetDailyRevenue()
            => Ok(await _orderService.GetDailyRevenueAsync());

        [HttpGet("dashboard/top-products")]
        public async Task<IActionResult> GetTopSellingProducts()
            => Ok(await _orderService.GetTopSellingProductsAsync());

        [HttpGet("dashboard/total-revenue")]
        public async Task<IActionResult> GetTotalRevenue()
            => Ok(new { TotalRevenue = await _orderService.GetTotalRevenueAsync() });

        [HttpGet("dashboard/monthly-orders")]
        public async Task<IActionResult> GetMonthlyOrderStats()
            => Ok(await _orderService.GetMonthlyOrderStatsAsync());

        [HttpGet("dashboard/order-count-today")]
        public async Task<IActionResult> GetTodayOrderCount()
            => Ok(new { Date = DateTime.Today.ToString("yyyy-MM-dd"), TotalOrders = await _orderService.GetTodayOrderCountAsync() });

        [HttpGet("dashboard/order-count-this-month")]
        public async Task<IActionResult> GetThisMonthOrderCount()
        {
            var now = DateTime.Now;
            var count = await _orderService.GetThisMonthOrderCountAsync();
            return Ok(new { Month = now.Month, Year = now.Year, TotalOrders = count });
        }

        [HttpGet("dashboard/my-revenue-today")]
        public async Task<IActionResult> GetTodayRevenue()
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
            var revenue = await _orderService.GetTodayRevenueByUserIdAsync(userId);
            return Ok(new { Date = DateTime.Today.ToString("yyyy-MM-dd"), Revenue = revenue });
        }


        [HttpGet("dashboard/my-revenue-this-month")]
        public async Task<IActionResult> GetThisMonthRevenue()
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
            var revenue = await _orderService.GetThisMonthRevenueByUserIdAsync(userId);
            return Ok(new { Month = DateTime.Now.Month, Year = DateTime.Now.Year, Revenue = revenue });
        }

        [HttpGet("dashboard/my-total-revenue")]
        public async Task<IActionResult> GetTotalAgencyRevenue()
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
            var revenue = await _orderService.GetTotalRevenueByUserIdAsync(userId);
            return Ok(new { TotalRevenue = revenue });
        }


        [HttpGet("dashboard/sales-order-count")]
        public async Task<IActionResult> GetSalesOrderCount()
        {
            var salesUserId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
            var count = await _orderService.GetOrderCountManagedBySalesAsync(salesUserId);
            return Ok(new { orderCount = count });
        }

        [HttpGet("dashboard/sales-real-revenue")]
        public async Task<IActionResult> GetSalesRealRevenue()
        {
            var salesUserId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
            var revenue = await _orderService.GetTotalRevenueManagedBySalesAsync(salesUserId);
            return Ok(new { realRevenue = revenue });
        }

        [HttpGet("dashboard/monthly-export-stats")]
        public async Task<IActionResult> GetMonthlyExportedOrderStats()
        {
            var stats = await _orderService.GetMonthlyExportedOrderStatsAsync();
            return Ok(new { success = true, data = stats });
        }




    }
}
