using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessObject.DTO.Warehouse;
using BusinessObject.Models;
using Microsoft.AspNetCore.SignalR;
using Repo.IRepository;
using Repo.Repository;
using Services.IService;

namespace Services.Service
{
    public class WarehouseTransferService : IWarehouseTransferService
    {
        private readonly ITemporaryWarehouseExportRepository _tempExportRepo;
        private readonly IWarehouseTransferRepository _transferRepo;
        private readonly IProductRepository _productRepository;
        private readonly IWarehouseExportRepository _exportReceiptRepo;
        private readonly IUserRepository _userRepository;
        private readonly INotificationRepository _notificationRepository;

        private readonly IHubContext<NotificationHub> _hub;

        public WarehouseTransferService(
            ITemporaryWarehouseExportRepository tempExportRepo,
            IWarehouseTransferRepository transferRepo,
            IProductRepository productRepository,
            IHubContext<NotificationHub> hub,
            IWarehouseExportRepository exportReceiptRepo,
            IUserRepository userRepository,
            INotificationRepository notificationRepository)
        {
            _tempExportRepo = tempExportRepo;
            _transferRepo = transferRepo;
            _productRepository = productRepository;
            _hub = hub;
            _exportReceiptRepo = exportReceiptRepo;
            _userRepository = userRepository;
            _notificationRepository = notificationRepository;
        }

        public DateTime GetVietnamTime()
        {
            // Lấy múi giờ Việt Nam (GMT+7)
            TimeZoneInfo vietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            DateTime vietnamTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vietnamTimeZone);
            return vietnamTime;
        }
        public async Task<ExportWarehouseReceipt> ApproveTransferRequestAndCreateReceiptAsync(int transferRequestId)
        {
            var random = new Random();
            var transferRequest = await _transferRepo.GetByIdAsync(transferRequestId);

            if (transferRequest == null || transferRequest.TransferProducts == null || !transferRequest.TransferProducts.Any())
                throw new InvalidOperationException("Yêu cầu điều phối không tìm thấy hoặc không hợp lệ.");

            if (transferRequest.Status == "Approved")
                throw new InvalidOperationException("Yêu cầu điều phối đã được phê duyệt.");

            // ✅ Tạo phiếu xuất kho điều phối từ kho phụ
            var exportReceipt = new ExportWarehouseReceipt
            {
                DocumentNumber = $"PXKDP{GetVietnamTime().Ticks}-{random.Next(1000, 9999)}",
                DocumentDate = GetVietnamTime(),
                ExportDate = GetVietnamTime(),
                ExportType = "ExportCoordination",
                Status = "Completed",
                WarehouseId = transferRequest.SourceWarehouseId,
                RequestExportId = transferRequest.RequestExportId, // không liên quan trực tiếp đến RequestExport
                ExportWarehouseReceiptDetails = new List<ExportWarehouseReceiptDetail>()
            };

            foreach (var product in transferRequest.TransferProducts)
            {
                var productInfo = await _productRepository.GetByIdAsync(product.ProductId);

                var tempExport = await _tempExportRepo.GetByProductAndBatchAsync(
                    product.ProductId,
                    product.BatchId.Value,
                    transferRequest.SourceWarehouseId
                );

                if (tempExport == null)
                    throw new InvalidOperationException("Không tìm thấy dữ liệu tạm thời (TemporaryStockExport) cho sản phẩm cần điều phối.");

                exportReceipt.ExportWarehouseReceiptDetails.Add(new ExportWarehouseReceiptDetail
                {
                    ProductId = product.ProductId,
                    ProductName = productInfo?.ProductName ?? "Unknown",
                    Quantity = product.Quantity,
                    UnitPrice = productInfo?.Price ?? 0,
                    TotalProductAmount = (productInfo?.Price ?? 0) * product.Quantity,
                    BatchId = product.BatchId.Value,
                    ExpiryDate = tempExport.ExpiryDate, // ✅ Lấy ngày hết hạn từ bảng tạm
                    BatchNumber = tempExport.BatchNumber,
                    WarehouseProductId = tempExport.WarehouseProductId, // ✅ Lấy ID lô hàng từ bảng tạm
                });
            }


            exportReceipt.TotalQuantity = exportReceipt.ExportWarehouseReceiptDetails.Sum(x => x.Quantity);
            exportReceipt.TotalAmount = exportReceipt.ExportWarehouseReceiptDetails.Sum(x => x.TotalProductAmount);

            await _exportReceiptRepo.AddRangeAsync(new List<ExportWarehouseReceipt> { exportReceipt });

            // ✅ Cập nhật trạng thái điều phối
            transferRequest.Status = "Approved";
            await _transferRepo.UpdateAsync(transferRequest);
            await _transferRepo.SaveChangesAsync();

            // ✅ Gửi thông báo đến user của kho nhận (DestinationWarehouseId)
            var destinationWarehouseId = transferRequest.DestinationWarehouseId;
            var userId = await _userRepository.GetUserIdByWarehouseIdAsync(destinationWarehouseId);

            if (userId != null)
            {
                string message = $"📦 Phiếu điều phối từ kho phụ đã được duyệt và đang chuyển hàng đến kho của bạn.";

                // Gửi qua SignalR
                await _hub.Clients.User(userId.ToString()).SendAsync("ReceiveNotification", new
                {
                    title = "Phiếu điều phối",
                    message,
                    payload = $"TransferRequestId: {transferRequestId}"
                });

                var timeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
                var vietnamNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);

                // Lưu DB
                var notification = new Notification
                {
                    UserId = userId.Value,
                    Title = "Phiếu điều phối",
                    Message = message,
                    Url = $"/warehouse/transfer-request",
                    CreatedAt = vietnamNow
                };

                await _notificationRepository.AddAsync(notification);
                await _notificationRepository.SaveChangesAsync();
            }

            return exportReceipt;
        }

        public async Task<List<WarehouseTransferRequestDetailDto>> GetAllTransferRequestsByUserAsync(Guid userId)
        {
            var entities = await _transferRepo.GetAllByUserIdAsync(userId);
            return entities.Select(ToDto).ToList();
        }

        public async Task<WarehouseTransferRequestDetailDto?> GetTransferRequestByIdAsync(long id, Guid userId)
        {
            var entity = await _transferRepo.GetByIdAndUserIdAsync(id, userId);
            return entity != null ? ToDto(entity) : null;
        }

        // Mapping helper
        private WarehouseTransferRequestDetailDto ToDto(WarehouseTransferRequest r)
        {
            return new WarehouseTransferRequestDetailDto
            {
                Id = r.Id,
                SourceWarehouseId = r.SourceWarehouseId,
                SourceWarehouseName = r.SourceWarehouse?.WarehouseName ?? "",
                DestinationWarehouseId = r.DestinationWarehouseId,
                WarehouseTranferCode = r.TranferRequestCode,
                DestinationWarehouseName = r.DestinationWarehouse?.WarehouseName ?? "",
                RequestExportId = r.RequestExportId,
                RequestDate = r.RequestDate,
                Notes = r.Notes,
                Status = r.Status,
                Products = r.TransferProducts.Select(p => new WarehouseTransferProductDto
                {
                    ProductId = p.ProductId,
                    ProductName = p.Product?.ProductName ?? "",
                    Quantity = p.Quantity,
                    BatchNumber = p.Batch?.BatchCode
                }).ToList()
            };
        }

        public async Task<List<WarehouseTransferRequestDetailDto>> GetBySourceWarehouseAsync(long sourceWarehouseId)
        {
            var list = await _transferRepo.GetBySourceWarehouseAsync(sourceWarehouseId);
            return list.Select(ToDto).ToList();
        }

        public async Task<List<WarehouseTransferRequestDetailDto>> GetByDestinationWarehouseAsync(long destinationWarehouseId)
        {
            var list = await _transferRepo.GetByDestinationWarehouseAsync(destinationWarehouseId);
            return list.Select(ToDto).ToList();
        }

    }
}
