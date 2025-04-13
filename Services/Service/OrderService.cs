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
        private readonly IExportRepository _exportRepository;
        private readonly IRequestProductRepository _requestProductRepository;
        private readonly IUserRepository _userRepository;
        private readonly IHubContext<NotificationHub> _hub;
        private readonly IPaymentHistoryRepository _paymentHistoryRepository;
        private readonly IInventoryService _iventoryService;
        private readonly ITemporaryWarehouseExportRepository _tempExportRepo;

        public OrderService(
            IOrderRepository orderRepository,
            IExportRepository exportRepository,
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
                    //ApprovedBy = approvedBy,  // ✅ Lấy EmployeeId từ User đăng nhập
                    ApprovedDate = DateTime.Now,
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



    }

}
