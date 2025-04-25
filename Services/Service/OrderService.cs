using BusinessObject.DTO.Order;
using BusinessObject.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Repo.IRepository;
using Repo.Repository;
using Services.IService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Services.Service
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IRequestExportRepository _exportRepository;
        private readonly IRequestProductRepository _requestProductRepository;
        private readonly IUserRepository _userRepository;
        private readonly IHubContext<NotificationHub> _hub;
        private readonly IPaymentHistoryRepository _paymentHistoryRepository;
        private readonly IInventoryService _iventoryService;
        private readonly ITemporaryWarehouseExportRepository _tempExportRepo;

        public OrderService(
            IOrderRepository orderRepository,
            IRequestExportRepository exportRepository,
            IRequestProductRepository requestProductRepository,
            IUserRepository agencyRepository,
            IHubContext<NotificationHub> hub,
            IPaymentHistoryRepository paymentHistoryRepository
            ,IInventoryService inventoryService,
            ITemporaryWarehouseExportRepository tempExportRepo)
        {
            _orderRepository = orderRepository;
            _exportRepository = exportRepository;
            _requestProductRepository = requestProductRepository;
            _userRepository = agencyRepository;
            _hub = hub;
            _paymentHistoryRepository = paymentHistoryRepository;
            _iventoryService = inventoryService;
            _tempExportRepo = tempExportRepo;
        }
        public async Task<List<OrderDto>> GetAllOrdersAsync()
        {
            var orders = await _orderRepository.GetAllOrdersAsync();

            return orders.Select(o => new OrderDto
            {
                OrderId = o.OrderId,
                OrderCode = o.OrderCode,
                OrderDate = o.OrderDate,
                Discount = o.Discount,
                FinalPrice = o.FinalPrice,
                Status = o.Status,

                // ✅ Thêm AgencyId
                AgencyId = o.RequestProduct?.AgencyId ?? 0, // nếu AgencyId là long


                // ✅ Thông tin request
                RequestCode = o.RequestProduct?.RequestCode ?? "N/A",
                AgencyName = o.RequestProduct?.AgencyAccount?.AgencyName ?? "Unknown",

                // ✅ Chi tiết đơn hàng
                OrderDetails = o.OrderDetails.Select(od => new OrderDetailDto
                {
                    OrderDetailId = od.OrderDetailId,
                    OrderId = od.OrderId,
                    ProductId = od.ProductId,
                    ProductName = od.Product?.ProductName ?? "N/A",
                    Quantity = od.Quantity,
                    UnitPrice = od.UnitPrice,
                    TotalAmount = od.TotalAmount,
                    Unit = od.Unit,
                    CreatedAt = od.CreatedAt
                }).ToList()

            }).ToList();
        }


        /*public async Task<Order> GetOrderByIdAsync(Guid orderId)
        {
            return await _orderRepository.GetOrderByIdAsync(orderId);
        }*/

        public async Task<OrderDto> GetOrderByIdAsync(Guid orderId)
        {
            var order = await _orderRepository.GetOrderByIdAsync(orderId);

            if (order == null)
            {
                throw new KeyNotFoundException($"Order with ID {orderId} not found.");
            }

            return new OrderDto
            {
                OrderId = order.OrderId,
                OrderCode = order.OrderCode,
                OrderDate = order.OrderDate,
                Discount = order.Discount,
                FinalPrice = order.FinalPrice,
                Status = order.Status,
                // ✅ Thêm AgencyId
                AgencyId = order.RequestProduct?.AgencyId ?? 0, // nếu AgencyId là long
                AgencyName = order.RequestProduct?.AgencyAccount?.AgencyName ?? "Unknown",
                RequestCode = order.RequestProduct?.RequestCode ?? "N/A",

                OrderDetails = order.OrderDetails.Select(od => new OrderDetailDto
                {
                    OrderDetailId = od.OrderDetailId,
                    OrderId = od.OrderId,
                    ProductId = od.ProductId,
                    ProductName = od.Product?.ProductName ?? "N/A",
                    Quantity = od.Quantity,
                    UnitPrice = od.UnitPrice,
                    TotalAmount = od.TotalAmount,
                    Unit = od.Unit,
                    CreatedAt = od.CreatedAt
                }).ToList()
            };
        }

        public async Task<bool> ProcessPaymentAsync(Guid orderId)
        {
            try
            {
                // ✅ Lấy Order từ OrderId
                var order = await _orderRepository.GetOrderByIdAsync(orderId);
                string requestExportCode = await _orderRepository.GenerateRequestExportCodeAsync();
                if (order == null || order.Status != "WaitPaid")
                    throw new Exception("Order not found or is not in a valid state.");

                // ✅ Lấy RequestProduct từ RequestId của Order
                var requestProduct = await _requestProductRepository.GetRequestProductByRequestIdAsync(order.RequestId);
                if (requestProduct == null)
                    throw new Exception("RequestProduct not found.");

                // ✅ Lấy `AgencyId` từ RequestProduct (RequestBy)
                long requestBy = requestProduct.AgencyId; // ✅ Lưu vào RequestExport.RequestedBy
                /*
                                // ✅ Lấy UserId từ JWT Token
                                var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                                if (string.IsNullOrEmpty(userId))
                                    throw new Exception("User ID not found in token.");*/

                var userId = await _userRepository.GetUserIdByAgencyIdAsync(requestBy);
                if (userId == null)
                    throw new Exception("User not found for the given AgencyId.");

                // ✅ Kiểm tra tổng nợ hiện tại
                var totalDebt = await _paymentHistoryRepository.GetTotalRemainingDebtAmountByUserIdAsync(userId.Value);

                // ✅ Kiểm tra giới hạn công nợ
                var creditLimit = await _paymentHistoryRepository.GetCreditLimitByUserIdAsync(userId.Value);

                // Nếu tổng nợ vượt hoặc bằng giới hạn công nợ, từ chối thanh toán
                if (creditLimit.HasValue && totalDebt >= creditLimit.Value)
                    throw new Exception("Bạn cần thanh toán các công nợ hiện tại trước khi tiếp tục.");



                /*// ✅ Lấy EmployeeId từ UserId thông qua UserRepository
                var employeeId = requestProduct.ApprovedBy;
                if (employeeId == null)
                    throw new Exception("Employee not found for the logged-in user.");

                long approvedBy = employeeId.Value; // ✅ Lưu vào RequestExport.ApprovedBy
*/
                // ✅ Tạo RequestExport từ Order
                var requestExport = new RequestExport
                {
                    RequestedByAgencyId = requestBy,  // ✅ Lấy AgencyId từ RequestProduct
                    RequestDate = requestProduct.CreatedAt,
                    Status = "Processing",
                    Note = "Order approved and exported",
                    OrderId = order.OrderId,
                    RequestExportCode = requestExportCode,
                };

                // ✅ Lưu RequestExport vào database
                await _exportRepository.AddExportAsync(requestExport);
                await _exportRepository.SaveChangesAsync(); // 🔥 Lưu để lấy RequestExportId

                // ✅ Lấy danh sách OrderDetails từ OrderId và lưu vào RequestExportDetail
                var requestExportDetails = order.OrderDetails
                    .Select(od => new RequestExportDetail
                    {
                        RequestExportId = requestExport.RequestExportId,
                        ProductId = od.ProductId,
                        RequestedQuantity = od.Quantity
                    }).ToList();

                // ✅ Lưu danh sách RequestExportDetail vào database
                await _exportRepository.AddExportDetailsAsync(requestExportDetails);
                await _exportRepository.SaveChangesAsync();

                // ✅ Cập nhật trạng thái đơn hàng
                order.Status = "Paid";
                requestProduct.RequestStatus = "Paid";
                await _orderRepository.UpdateOrderAsync(order);
                await _orderRepository.SaveChangesAsync();

                /*// Gửi cho Sale
                await _hub.Clients.Group("4")
                    .SendAsync("ReceiveNotification", $"🚚 Có Đơn Hàng Mới Được Thanh Toán!");*/

                var notification = new
                {
                    title = "Sales", // Tiêu đề thông báo
                    message = "🚚 Có Đơn Hàng Mới Được Thanh Toán!", // Nội dung thông báo
                    payload = order.OrderCode // Có thể thêm mã đơn hàng hoặc thông tin chi tiết nếu cần
                };

                // Gửi thông báo qua SignalR cho Sale
                await _hub.Clients.Group("4")
                    .SendAsync("ReceiveNotification", notification);

                return true;
            }
            catch (DbUpdateException ex) // ✅ Bắt lỗi từ Entity Framework
            {
                throw new Exception($"Database update failed: {ex.InnerException?.Message}", ex);
            }
            catch (Exception ex) // ✅ Bắt lỗi tổng quát
            {
                throw new Exception($"An error occurred: {ex.Message}", ex);
            }
        }




        public async Task<bool> CancelOrderAsync(Guid orderId)
        {
            var order = await _orderRepository.GetOrderByIdAsync(orderId);
            var requestProduct = await _requestProductRepository.GetRequestByIdAsync(order.RequestId);
            if (order == null || order.Status == "Paid") return false;
            
            if(order.Status == "WaitPaid")
            {
                requestProduct.RequestStatus = "Canceled";
                order.Status = "Canceled";
                await _orderRepository.UpdateOrderAsync(order);
               await _orderRepository.SaveAsync();

                await _iventoryService.RollbackStockForCancelledOrderAsync(orderId);
            }
            return true;
        }

        /*public async Task<List<Order>> GetOrdersByAgencyIdAsync(long agencyId)
        {
            return await _orderRepository.GetOrdersByAgencyIdAsync(agencyId);
        }*/

        public async Task<List<OrderDto>> GetOrdersByAgencyIdAsync(long agencyId)
        {
            var orders = await _orderRepository.GetOrdersByAgencyIdAsync(agencyId);

            return orders.Select(o => new OrderDto
            {
                OrderId = o.OrderId,
                OrderCode = o.OrderCode,
                OrderDate = o.OrderDate,
                Discount = o.Discount,
                FinalPrice = o.FinalPrice,
                Status = o.Status,
                // ✅ Thêm AgencyId
                AgencyId = o.RequestProduct?.AgencyId ?? 0, // nếu AgencyId là long
                AgencyName = o.RequestProduct?.AgencyAccount?.AgencyName ?? "Unknown",
                RequestCode = o.RequestProduct?.RequestCode ?? "N/A",

                OrderDetails = o.OrderDetails.Select(od => new OrderDetailDto
                {
                    OrderDetailId = od.OrderDetailId,
                    OrderId = od.OrderId,
                    ProductId = od.ProductId,
                    ProductName = od.Product?.ProductName ?? "N/A",
                    Quantity = od.Quantity,
                    UnitPrice = od.UnitPrice,
                    TotalAmount = od.TotalAmount,
                    Unit = od.Unit,
                    CreatedAt = od.CreatedAt
                }).ToList()

            }).ToList();
        }


        public async Task<Order> GetOrderByOrderCodeAsync(string orderCode)
        {
            return await _orderRepository.GetOrderByOrderCodeAsync(orderCode);
        }


        public async Task<List<object>> GetOrderStatusCountsAsync()
        {
            var orders = await _orderRepository.GetAllOrdersAsync();
            return orders.GroupBy(o => o.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .Cast<object>()
                .ToList();
        }

        public async Task<List<object>> GetDailyRevenueAsync()
        {
            var payments = await _paymentHistoryRepository.GetAllAsync();

            return payments
                .Where(p => (p.Status == "FULL_PAID" || p.Status == "PARTIALLY_PAID") &&
                            p.PaymentDate >= DateTime.Now.AddDays(-7))
                .GroupBy(p => p.PaymentDate.Date)
                .Select(g => new {
                    Date = g.Key,
                    TotalRevenue = g.Sum(p => p.PaymentAmount)
                })
                .OrderBy(x => x.Date)
                .Cast<object>()
                .ToList();
        }


        public async Task<List<object>> GetTopSellingProductsAsync()
        {
            var orders = await _orderRepository.GetAllOrdersAsync();

            return orders
                .Where(o => o.Status == "Paid" || o.Status == "WaitingDelivery")
                .SelectMany(o => o.OrderDetails)
                .GroupBy(od => new { od.ProductId, od.Product.ProductName })
                .Select(g => new { g.Key.ProductId, g.Key.ProductName, TotalSold = g.Sum(od => od.Quantity) })
                .OrderByDescending(p => p.TotalSold)
                .Take(5)
                .Cast<object>()
                .ToList();
        }

        public async Task<decimal> GetTotalRevenueAsync()
        {
            var payments = await _paymentHistoryRepository.GetAllAsync();

            return payments
                .Where(p => p.Status == "FULL_PAID" || p.Status == "PARTIALLY_PAID")
                .Sum(p => p.PaymentAmount);
        }


        public async Task<List<object>> GetMonthlyOrderStatsAsync()
        {
            var orders = await _orderRepository.GetAllOrdersAsync();
            var payments = await _paymentHistoryRepository.GetAllAsync();
            var currentYear = DateTime.Now.Year;

            return orders
                .Where(o => o.OrderDate.Year == currentYear &&
                            (o.Status == "Paid" || o.Status == "WaitingDelivery"))
                .GroupBy(o => o.OrderDate.Month)
                .Select(g => new {
                    Month = g.Key,
                    TotalOrders = g.Count(),
                    TotalRevenue = payments
                        .Where(p => g.Select(o => o.OrderId).Contains(p.OrderId) &&
                                    (p.Status == "FULL_PAID" || p.Status == "PARTIALLY_PAID"))
                        .Sum(p => p.PaymentAmount)
                })
                .Cast<object>()
                .ToList();
        }


        public async Task<int> GetTodayOrderCountAsync()
        {
            var today = DateTime.Today;
            var orders = await _orderRepository.GetAllOrdersAsync();
            return orders.Count(o => o.OrderDate.Date == today && (o.Status == "Paid" || o.Status == "WaitingDelivery"));
        }

        public async Task<int> GetThisMonthOrderCountAsync()
        {
            var now = DateTime.Now;
            var orders = await _orderRepository.GetAllOrdersAsync();
            return orders.Count(o => o.OrderDate.Month == now.Month && o.OrderDate.Year == now.Year && (o.Status == "Paid" || o.Status == "WaitingDelivery"));
        }

        public async Task<decimal> GetTodayRevenueByUserIdAsync(Guid userId)
        {
            var payments = await _paymentHistoryRepository.GetAllAsync();
            var today = DateTime.Today;

            return payments
                .Where(p => p.UserId == userId &&
                            (p.Status == "FULL_PAID" || p.Status == "PARTIALLY_PAID") &&
                            p.PaymentDate.Date == today)
                .Sum(p => p.PaymentAmount);
        }

        public async Task<decimal> GetThisMonthRevenueByUserIdAsync(Guid userId)
        {
            var payments = await _paymentHistoryRepository.GetAllAsync();
            var now = DateTime.Now;

            return payments
                .Where(p => p.UserId == userId &&
                            (p.Status == "FULL_PAID" || p.Status == "PARTIALLY_PAID") &&
                            p.PaymentDate.Month == now.Month &&
                            p.PaymentDate.Year == now.Year)
                .Sum(p => p.PaymentAmount);
        }

        public async Task<decimal> GetTotalRevenueByUserIdAsync(Guid userId)
        {
            var payments = await _paymentHistoryRepository.GetAllAsync();

            return payments
                .Where(p => p.UserId == userId &&
                            (p.Status == "FULL_PAID" || p.Status == "PARTIALLY_PAID"))
                .Sum(p => p.PaymentAmount);
        }



    }

}
