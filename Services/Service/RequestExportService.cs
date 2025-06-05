using BusinessObject.DTO.RequestExport;
using BusinessObject.Models;
using Microsoft.AspNetCore.SignalR;
using Repo.IRepository;
using Repo.Repository;
using Services.IService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Service
{
    public class RequestExportService : IRequestExportService
    {
        private readonly IRequestExportRepository _requestExportRepository;
        private readonly ITemporaryWarehouseExportRepository _temporaryWarehouseRepository;
        private readonly IRequestProductRepository _requestProductRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly IUserRepository _userRepository;
        private readonly IEmailService _emailService;
        private readonly INotificationRepository _notificationRepository;
        private readonly IHubContext<NotificationHub> _hub;
        private readonly IInventoryService _inventoryService;
        private readonly IPaymentHistoryRepository _paymentRepository;

        public RequestExportService(IRequestExportRepository requestExportRepository
            , ITemporaryWarehouseExportRepository temporaryWarehouseRepository,
                IOrderRepository orderRepository,
                IRequestProductRepository productRepository,
                IUserRepository userRepository,
                IEmailService emailService,
                IHubContext<NotificationHub> hub,
            INotificationRepository notificationRepository,
            IInventoryService inventoryService,
            IPaymentHistoryRepository paymentRepository)
        {
            _requestExportRepository = requestExportRepository;
            _temporaryWarehouseRepository = temporaryWarehouseRepository;
            _orderRepository = orderRepository;
            _requestProductRepository = productRepository;
            _userRepository = userRepository;
            _emailService = emailService;
            _hub = hub;
            _notificationRepository = notificationRepository;
            _inventoryService = inventoryService;
            _paymentRepository = paymentRepository;
        }

        public async Task<List<RequestExportDto>> GetAllRequestExportsAsync(string? sortBy = null)
        {
            var requestExports = await _requestExportRepository.GetAllRequestExportsAsync();

            // Lấy danh sách OrderId duy nhất từ RequestExport
            var orderIds = requestExports
                .Select(x => x.OrderId)
                .Distinct()
                .ToList();

            // Gọi repository để lấy toàn bộ TemporaryStockExport theo các OrderId
            var tempExports = await _temporaryWarehouseRepository.GetByOrderIdsAsync(orderIds);

            // Group theo OrderId để ánh xạ nhanh hơn
            var tempExportDict = tempExports
                .GroupBy(t => t.OrderId)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Sắp xếp nếu cần
            var statusPriority = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
                {
                    { "Pending", 0 },
                    { "Requested", 1 },
                    { "Approved", 2 }
                };

            if (!string.IsNullOrEmpty(sortBy))
            {
                switch (sortBy.ToLower())
                {
                    case "status":
                        requestExports = requestExports
                            .OrderBy(re => statusPriority.ContainsKey(re.Status) ? statusPriority[re.Status] : 99)
                            .ToList();
                        break;
                    case "requestdate_desc":
                        requestExports = requestExports
                            .OrderByDescending(re => re.RequestDate)
                            .ToList();
                        break;
                    case "requestdate_asc":
                        requestExports = requestExports
                            .OrderBy(re => re.RequestDate)
                            .ToList();
                        break;
                }
            }

            // Ánh xạ sang DTO
            return requestExports.Select(re => new RequestExportDto
            {
                RequestExportId = re.RequestExportId,
                OrderId = re.OrderId,
                AgencyName = re.RequestedByAgency?.AgencyName ?? "Unknown",
                RequestDate = re.RequestDate,
                Status = re.Status,
                Note = re.Note,
                RequestExportCode = re.RequestExportCode,
                Reason = re.Reason,
                RequestExportDetails = re.RequestExportDetails.Select(red => new RequestExportDetailDto
                {
                    RequestExportDetailId = red.RequestItemId,
                    ProductId = red.ProductId,
                    ProductName = red.Product?.ProductName ?? "N/A",
                    Unit = red.Product?.Unit ?? "N/A",
                    Price = red.Product?.Price ?? 0,
                    RequestedQuantity = red.RequestedQuantity
                }).ToList(),

                TemporaryStockExportDetails = tempExportDict.ContainsKey(re.OrderId)
                    ? tempExportDict[re.OrderId].Select(tse => new TemporaryStockExportDto
                        {
                            WarehouseId = tse.WarehouseId,
                            ProductId = tse.ProductId,
                            BatchId = tse.BatchId,
                            Quantity = tse.Quantity
                        }).ToList()
    :                   new List<TemporaryStockExportDto>()

            }).ToList();
        }


        public async Task<RequestExportDto> GetRequestExportByIdAsync(int requestId)
        {
            var requestExport = await _requestExportRepository.GetRequestExportById(requestId);

            if (requestExport == null)
            {
                return null;
            }

            // ✅ Lấy dữ liệu TemporaryStockExport theo OrderId
            var tempExports = await _temporaryWarehouseRepository.GetByOrderIdAsync(requestExport.OrderId);

            return new RequestExportDto
            {
                RequestExportId = requestExport.RequestExportId,
                OrderId = requestExport.OrderId,
                AgencyName = requestExport.RequestedByAgency?.AgencyName ?? "Unknown",
                RequestDate = requestExport.RequestDate,
                Status = requestExport.Status,
                Note = requestExport.Note,
                WarehouseId = requestExport.Order?.TemporaryStockExports?.FirstOrDefault()?.WarehouseId ?? 0,
                WarehouseName = requestExport.Order?.TemporaryStockExports?.FirstOrDefault()?.Warehouse?.WarehouseName ?? "Unknown",
                RequestExportCode = requestExport.RequestExportCode,
                Reason = requestExport.Reason,
                RequestExportDetails = requestExport.RequestExportDetails != null
                    ? requestExport.RequestExportDetails.Select(red => new RequestExportDetailDto
                    {
                        RequestExportDetailId = red.RequestItemId,
                        ProductId = red.ProductId,
                        ProductName = red.Product?.ProductName ?? "N/A",
                        Unit = red.Product?.Unit ?? "N/A",
                        Price = red.Product?.Price ?? 0,
                        RequestedQuantity = red.RequestedQuantity
                    }).ToList()
                    : new List<RequestExportDetailDto>(),

                TemporaryStockExportDetails = tempExports != null && tempExports.Any()
                    ? tempExports.Select(tse => new TemporaryStockExportDto
                    {
                        WarehouseId = tse.WarehouseId,
                        ProductId = tse.ProductId,
                        BatchId = tse.BatchId,
                        Quantity = tse.Quantity
                    }).ToList()
                    : new List<TemporaryStockExportDto>()
            };
        }


        public async Task<List<RequestExportDto>> GetRequestExportsBySalesAsync(Guid salesUserId, string? sortBy = null)
        {
            var requestExports = await _requestExportRepository.GetRequestExportsBySalesUserIdAsync(salesUserId);

            var orderIds = requestExports.Select(x => x.OrderId).Distinct().ToList();
            var tempExports = await _temporaryWarehouseRepository.GetByOrderIdsAsync(orderIds);
            var tempExportDict = tempExports.GroupBy(t => t.OrderId).ToDictionary(g => g.Key, g => g.ToList());

            // Sắp xếp theo yêu cầu
            var statusPriority = new Dictionary<string, int> {
        { "Pending", 0 }, { "Requested", 1 }, { "Approved", 2 }
    };

            if (!string.IsNullOrEmpty(sortBy))
            {
                requestExports = sortBy.ToLower() switch
                {
                    "status" => requestExports.OrderBy(re => statusPriority.ContainsKey(re.Status) ? statusPriority[re.Status] : 99).ToList(),
                    "requestdate_desc" => requestExports.OrderByDescending(re => re.RequestDate).ToList(),
                    "requestdate_asc" => requestExports.OrderBy(re => re.RequestDate).ToList(),
                    _ => requestExports
                };
            }

            return requestExports.Select(re =>
            {
                var (warehouseId, warehouseName) = tempExportDict.ContainsKey(re.OrderId)
                    ? GetPrimaryWarehouse(re.OrderId, tempExportDict[re.OrderId])
                    : (0, "Unknown");

                return new RequestExportDto
                {
                    RequestExportId = re.RequestExportId,
                    OrderId = re.OrderId,
                    OrderCode = re.Order.OrderCode,
                    AgencyName = re.RequestedByAgency?.AgencyName ?? "Unknown",
                    RequestDate = re.RequestDate,
                    Status = re.Status,
                    Note = re.Note,
                    RequestExportCode = re.RequestExportCode,
                    WarehouseId = warehouseId,
                    WarehouseName = warehouseName,
                    Discount = re.Discount,
                    TotalPrice = re.TotalPrice,
                    FinalPrice = re.FinalPrice,
                    Reason = re.Reason,
                    RequestExportDetails = re.RequestExportDetails.Select(red => new RequestExportDetailDto
                    {
                        RequestExportDetailId = red.RequestItemId,
                        ProductId = red.ProductId,
                        ProductName = red.Product?.ProductName ?? "N/A",
                        Unit = red.Unit,
                        Price = red.SellingPrice,
                        RequestedQuantity = red.RequestedQuantity
                    }).ToList(),
                    TemporaryStockExportDetails = tempExportDict.ContainsKey(re.OrderId)
                        ? tempExportDict[re.OrderId].Select(tse => new TemporaryStockExportDto
                        {
                            WarehouseId = tse.WarehouseId,
                            ProductId = tse.ProductId,
                            BatchId = tse.BatchId,
                            Quantity = tse.Quantity
                        }).ToList()
                        : new List<TemporaryStockExportDto>()
                };
            }).ToList();
        }


        public async Task<RequestExportDto?> GetRequestExportByIdForSalesAsync(int requestId, Guid salesUserId)
        {
            var re = await _requestExportRepository.GetRequestExportByIdAsync(requestId, salesUserId);
            if (re == null) return null;

            var tempExports = await _temporaryWarehouseRepository.GetByOrderIdAsync(re.OrderId);
            var (warehouseId, warehouseName) = GetPrimaryWarehouse(re.OrderId, tempExports);

            return new RequestExportDto
            {
                RequestExportId = re.RequestExportId,
                OrderId = re.OrderId,
                OrderCode = re.Order.OrderCode,
                AgencyName = re.RequestedByAgency?.AgencyName ?? "Unknown",
                RequestDate = re.RequestDate,
                Status = re.Status,
                Note = re.Note,
                WarehouseId = warehouseId,
                WarehouseName = warehouseName,
                RequestExportCode = re.RequestExportCode,
                Reason = re.Reason,
                RequestExportDetails = re.RequestExportDetails.Select(red => new RequestExportDetailDto
                {
                    RequestExportDetailId = red.RequestItemId,
                    ProductId = red.ProductId,
                    ProductName = red.Product?.ProductName ?? "N/A",
                    Unit = red.Product?.Unit ?? "N/A",
                    Price = red.Product?.Price ?? 0,
                    RequestedQuantity = red.RequestedQuantity
                }).ToList(),
                TemporaryStockExportDetails = tempExports.Select(tse => new TemporaryStockExportDto
                {
                    WarehouseId = tse.WarehouseId,
                    ProductId = tse.ProductId,
                    BatchId = tse.BatchId,
                    Quantity = tse.Quantity
                }).ToList()
            };
        }

        private (long warehouseId, string warehouseName) GetPrimaryWarehouse(Guid orderId, List<TemporaryStockExport> tempExports)
        {
            if (tempExports == null || !tempExports.Any())
                return (0, "Unknown");

            var mainWarehouse = tempExports
                .Where(x => x.OrderId == orderId)
                .GroupBy(x => x.WarehouseId)
                .Select(g => new
                {
                    WarehouseId = g.Key,
                    TotalQuantity = g.Sum(x => x.Quantity),
                    WarehouseName = g.FirstOrDefault()?.Warehouse?.WarehouseName ?? "Unknown"
                })
                .OrderByDescending(x => x.TotalQuantity)
                .FirstOrDefault();

            return mainWarehouse != null
                ? (mainWarehouse.WarehouseId, mainWarehouse.WarehouseName)
                : (0, "Unknown");
        }

        public async Task CancelRequestExportAsync(int requestExportId, Guid userId, string reason)
        {
            // 1. Lấy RequestExport
            var requestExport = await _requestExportRepository.GetRequestExportByIdAsync(requestExportId)
                ?? throw new Exception("Không tìm thấy đơn xuất kho.");

            // 2. Lấy Order liên quan
            var order = await _orderRepository.GetOrderByIdAsync(requestExport.OrderId)
                ?? throw new Exception("Không tìm thấy đơn đặt hàng liên quan.");

            // 3. Lấy RequestProduct liên quan
            var requestProduct = await _requestProductRepository.GetRequestProductByRequestIdAsync(order.RequestId)
                ?? throw new Exception("Không tìm thấy yêu cầu sản phẩm liên quan.");

            var paymentHistory = await _paymentRepository.GetPaymentHistoryByOrderIdAsync(order.OrderId)
                ?? throw new Exception("Không tìm thấy lịch sử thanh toán.");

            // 4. Set status = "Canceled"
            requestExport.Status = "Canceled";
            requestExport.Reason = reason; // Lưu lý do hủy
            order.Status = "Canceled";
            order.Reason = reason; // Lưu lý do hủy
            requestProduct.RequestStatus = "Canceled";

            await _inventoryService.RollbackStockForCancelledOrderAsync(order.OrderId);

            // 5. Update
            await _requestExportRepository.UpdateExportAsync(requestExport);
            await _orderRepository.UpdateOrderAsync(order);
            await _requestProductRepository.UpdateRequestAsync(requestProduct);
            await _paymentRepository.SetPaymentHistoryStatusByIdAsync(paymentHistory.PaymentHistoryId, "CANCELLED");
            await _requestExportRepository.SaveChangesAsync();

            var agencyId = requestProduct.AgencyId;
            // 3. Lấy AgencyAccount (hoặc bảng đại lý) từ AgencyId
            var agencyAccount = await _userRepository.GetAgencyAccountByIdAsync(agencyId)
                ?? throw new Exception("Không tìm thấy tài khoản đại lý.");

            var agencyUserId = agencyAccount.UserId; // Đổi tên biến
            var customerUser = await _userRepository.GetByIdAsync(agencyUserId)
                ?? throw new Exception("Không tìm thấy người dùng của đại lý.");

            // 6. Lấy email và tên
            var customerEmail = customerUser.Email;
            var customerName = agencyAccount.AgencyName; // hoặc user.FullName nếu có
            // ==== ĐẶT LỆNH GỬI EMAIL Ở ĐÂY ====
            await _emailService.SendOrderCancelNotificationEmailAsync(
                customerEmail,
                customerName,
                order.OrderCode,
                paymentHistory.PaymentAmount
            );

            var managerUserId = requestExport?.RequestedByAgency?.User.UserId;
            if (managerUserId != null && managerUserId != userId)
            {
                var salesName = requestExport.RequestedByAgency?.ManagedByEmployee?.FullName ?? "Không xác định";
                var orderCode = requestExport.Order?.OrderCode ?? "chưa có mã";

                string message = $"❌ Sales {salesName} đã hủy yêu cầu xuất kho cho đơn hàng {orderCode}.";

                await _hub.Clients.User(managerUserId.Value.ToString()).SendAsync("ReceiveNotification", new
                {
                    title = "huyDaily",
                    message,
                    payload = requestExportId
                });

                var timeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
                var vietnamNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);

                var notification = new Notification
                {
                    UserId = managerUserId.Value,
                    Title = "Yêu cầu xuất kho bị hủy",
                    Message = message,
                    Url = $"/agency/orders",
                    CreatedAt = vietnamNow
                };

                await _notificationRepository.AddAsync(notification);
                await _notificationRepository.SaveChangesAsync();
            }

        }

    }
}
