using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessObject.DTO;
using BusinessObject.DTO.ReturnOrder;
using BusinessObject.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Repo.IRepository;
using Repo.Repository;
using Services.IService;

namespace Services.Service
{
    public class DamagedStockService : IDamagedStockService
    {
        private readonly IDamagedStockRepository _damagedRepo;
        private readonly IReturnWarehouseReceiptRepository _returnWarehouseReceiptRepo;
        private readonly IReturnRequestRepository _returnRepo;
        private readonly IWarehouseRepository _warehouseRepo;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;
        private readonly IUserRepository _userRepo;
        private readonly IOrderRepository _orderRepo;
        private readonly INotificationRepository _notificationRepository;

        private readonly IHubContext<NotificationHub> _hub;

        public DamagedStockService(
            IDamagedStockRepository repo,
            IReturnWarehouseReceiptRepository receiptRepo,
            IReturnRequestRepository reqRepo,
            IWarehouseRepository whRepo,
            IConfiguration configuration,
            IEmailService emailService,
            IUserRepository userRepository,
            IOrderRepository orderRepo,
            IHubContext<NotificationHub> hub,
            INotificationRepository notificationRepository)
        {
            _damagedRepo = repo;
            _returnWarehouseReceiptRepo = receiptRepo;
            _returnRepo = reqRepo;
            _warehouseRepo = whRepo;
            _configuration = configuration;
            _emailService = emailService;
            _userRepo = userRepository;
            _orderRepo = orderRepo;
            _hub = hub;
            _notificationRepository = notificationRepository;
        }

        public Task<IEnumerable<DamagedStockDto>> GetByWarehouseIdAsync(long warehouseId)
        => _damagedRepo.GetByWarehouseIdAsync(warehouseId);

        /*public async Task ImportToDamagedStockAsync(long warehouseReceiptId, Guid warehouseUserId)
        {
            // 1) Lấy phiếu nhập trả hàng kèm CreatedByUser, ReturnRequest, Warehouse, Details
            // 1) Lấy phiếu trả hàng cùng chi tiết
            var receipt = await _returnWarehouseReceiptRepo
                .GetByIdWithDetailsAsync(warehouseReceiptId);


            var user = await _userRepo.GetUserByIdAsync(receipt.CreatedBy)
                ?? throw new KeyNotFoundException("Không tìm thấy người tạo phiếu.");

            var returnReceipt = await _returnRepo.GetByIdAsync(receipt.ReturnRequestId);

            if (returnReceipt.Status != "Approved")
                throw new Exception("Phiếu Trả Hàng Chưa Duyệt.");

            if (receipt.Status == "Completed")
                throw new Exception("Đơn Hàng Đã Được Xử Lý Thành Công Trước Đó!");

            // 5) Lấy warehouseId và kiểm quyền
            var userWarehouseId = await _warehouseRepo.GetWarehouseIdByUserAsync(warehouseUserId);
            if (userWarehouseId == 0 || userWarehouseId != receipt.WarehouseId)
                throw new UnauthorizedAccessException("Bạn không có quyền thao tác kho này.");

            // 6) Map chi tiết thành DamagedStock
            var now = DateTime.UtcNow;
            var damagedStocks = receipt.Details
         .Select(d =>
         {
             // Tìm ReturnRequestDetail tương ứng
             var detail = returnReceipt.Details.FirstOrDefault(rd => rd.ProductId == d.ProductId);
             if (detail == null)
                 throw new Exception($"Không tìm thấy chi tiết trả hàng cho sản phẩm {d.ProductId}");

             return new DamagedStock
             {
                 ProductId = d.ProductId,
                 WarehouseId = userWarehouseId,
                 Quantity = d.Quantity,
                 BatchId = d.BatchId,
                 CreatedAt = now,
                 Reason = detail.Reason ?? "DefectiveGood",  // ✅ Lấy từ ReturnRequestDetail
                 Status = "Return"
             };
         })
         .ToList();
            // 6) Lưu vào bảng DamagedStock
            await _damagedRepo.AddRangeAsync(damagedStocks);

            // 7) Cập nhật trạng thái phiếu nhập trả hàng
            await _returnWarehouseReceiptRepo.UpdateStatusAsync(warehouseReceiptId, "Imported");

            // 8) Cập nhật trạng thái ReturnRequest thành Completed
            await _returnRepo.UpdateStatusAsync(receipt.ReturnRequestId, "Completed");

            var request = await _returnRepo.GetByIdWithDetailsAsync(receipt.ReturnRequestId);
            if (request == null)
                throw new Exception("Không tìm thấy yêu cầu trả hàng.");

            var order = await _orderRepo.GetOrderByIdAsync(request.OrderId);
            if (order == null)
                throw new Exception("Không tìm thấy đơn hàng.");


            // 9) Gửi email thông báo tới người tạo phiếu
            var managerEmail = user.Email;
            var warehouseName = receipt.Warehouse.WarehouseName;
            await _emailService.SendDamagedStockNotificationEmailAsync(
                managerEmail,
                warehouseName,
                damagedStocks
            );

            // 🔔 Gửi thông báo đến đại lý
            var agencyUserId = order.RequestProduct?.AgencyAccount?.User?.UserId;

            if (agencyUserId != null)
            {
                string message = $"📦 Yêu cầu trả hàng cho đơn {order.OrderCode} đã được tiếp nhận và nhập kho.";

                // Gửi SignalR đến đại lý
                await _hub.Clients.User(agencyUserId.Value.ToString()).SendAsync("ReceiveNotification", new
                {
                    title = "ReturnAgency",
                    message,
                    payload = receipt.ReturnWarehouseReceiptId
                });

                // Lưu thông báo vào DB
                var timeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
                var vietnamNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);

                var notification = new Notification
                {
                    UserId = agencyUserId.Value,
                    Title = "Hoàn tất yêu cầu trả hàng",
                    Message = message,
                    Url = $"/agency/return-order",
                    CreatedAt = vietnamNow
                };

                await _notificationRepository.AddAsync(notification);
                await _notificationRepository.SaveChangesAsync();
            }

        }*/

        public async Task ImportToDamagedStockAsync(long warehouseReceiptId, Guid warehouseUserId)
        {
            var receipt = await _returnWarehouseReceiptRepo.GetByIdWithDetailsAsync(warehouseReceiptId);

            var user = await _userRepo.GetUserByIdAsync(receipt.CreatedBy)
                ?? throw new KeyNotFoundException("Không tìm thấy người tạo phiếu.");

            var returnReceipt = await _returnRepo.GetByIdWithDetailsAsync(receipt.ReturnRequestId);

            if (returnReceipt.Status != "Approved")
                throw new Exception("Phiếu Trả Hàng Chưa Duyệt.");

            if (receipt.Status == "Completed")
                throw new Exception("Đơn Hàng Đã Được Xử Lý Thành Công Trước Đó!");

            var userWarehouseId = await _warehouseRepo.GetWarehouseIdByUserAsync(warehouseUserId);
            if (userWarehouseId == 0 || userWarehouseId != receipt.WarehouseId)
                throw new UnauthorizedAccessException("Bạn không có quyền thao tác kho này.");

            var order = await _orderRepo.GetOrderByIdAsync(returnReceipt.OrderId);
            if (order == null)
                throw new Exception("Không tìm thấy đơn hàng.");

            var now = DateTime.UtcNow;
            var damagedStocks = new List<DamagedStock>();
            decimal totalAmount = 0;

            foreach (var d in receipt.Details)
            {
                var detail = returnReceipt.Details.FirstOrDefault(rd => rd.ProductId == d.ProductId);
                if (detail == null)
                    throw new Exception($"Không tìm thấy chi tiết trả hàng cho sản phẩm {d.ProductId}");

                var unitPrice = await _orderRepo.GetUnitPriceAsync(order.OrderId, d.ProductId);
                if (unitPrice == null)
                    throw new Exception($"Không tìm thấy đơn giá cho sản phẩm {d.ProductId} trong đơn hàng.");

                totalAmount += unitPrice.Value * d.Quantity;

                damagedStocks.Add(new DamagedStock
                {
                    ProductId = d.ProductId,
                    WarehouseId = userWarehouseId,
                    Quantity = d.Quantity,
                    BatchId = d.BatchId,
                    CreatedAt = now,
                    Reason = detail.Reason ?? "DefectiveGood",
                    Status = "Return"
                });
            }

            await _damagedRepo.AddRangeAsync(damagedStocks);
            await _returnWarehouseReceiptRepo.UpdateStatusAsync(warehouseReceiptId, "Imported");
            await _returnRepo.UpdateStatusAsync(receipt.ReturnRequestId, "Completed");

            var managerEmail = user.Email;
            var warehouseName = receipt.Warehouse.WarehouseName;

            await _emailService.SendDamagedStockNotificationEmailAsync(
                managerEmail,
                warehouseName,
                totalAmount,// 🧮 Tổng tiền hàng bị hư
                damagedStocks
                 
            );

            var agencyUserId = order.RequestProduct?.AgencyAccount?.User?.UserId;

            if (agencyUserId != null)
            {
                string message = $"📦 Yêu cầu trả hàng cho đơn {order.OrderCode} đã được tiếp nhận và nhập kho.";

                await _hub.Clients.User(agencyUserId.Value.ToString()).SendAsync("ReceiveNotification", new
                {
                    title = "ReturnAgency",
                    message,
                    payload = receipt.ReturnWarehouseReceiptId
                });

                var timeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
                var vietnamNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);

                var notification = new Notification
                {
                    UserId = agencyUserId.Value,
                    Title = "Hoàn tất yêu cầu trả hàng",
                    Message = message,
                    Url = $"/agency/return-order",
                    CreatedAt = vietnamNow
                };

                await _notificationRepository.AddAsync(notification);
                await _notificationRepository.SaveChangesAsync();
            }
        }


        public async Task<IEnumerable<GetDamagedStockDto>> GetByUserWarehouseAsync(Guid userId)
        {
            return await _damagedRepo.GetByUserWarehouseAsync(userId);
        }
    }
}
