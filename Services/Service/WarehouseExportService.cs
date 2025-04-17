using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessObject.Models;
using Microsoft.AspNetCore.SignalR;
using Repo.IRepository;
using Services.IService;

namespace Services.Service
{
    public class WarehouseExportService : IWarehouseExportService
    {
        private readonly ITemporaryWarehouseExportRepository _tempExportRepo;
        private readonly IWarehouseTransferRepository _transferRepo;
        private readonly IProductRepository _productRepository;
        private readonly IRequestExportRepository _requestExportRepository;
        private readonly IWarehouseExportRepository _exportReceiptRepo;

        private readonly IHubContext<NotificationHub> _hub;

        public WarehouseExportService(
            ITemporaryWarehouseExportRepository tempExportRepo,
            IWarehouseTransferRepository transferRepo,
            IProductRepository productRepository,
            IHubContext<NotificationHub> hub,
            IRequestExportRepository requestExportRepository,
            IWarehouseExportRepository exportReceiptRepo)
        {
            _tempExportRepo = tempExportRepo;
            _transferRepo = transferRepo;
            _productRepository = productRepository;
            _hub = hub;
            _requestExportRepository = requestExportRepository;
            _exportReceiptRepo = exportReceiptRepo;
        }

        public async Task<ExportWarehouseReceipt> CreateExportReceiptForMainWarehouseAsync(int requestExportId, Guid currentUserId)
        {
            var requestExport = await _requestExportRepository.GetRequestExportByIdAsync(requestExportId);
            if (requestExport == null || requestExport.RequestExportDetails == null || !requestExport.RequestExportDetails.Any())
                throw new InvalidOperationException("RequestExport not found or invalid.");

            if (requestExport.Status == "Requested" || requestExport.Status == "Approved")
                throw new InvalidOperationException("This request has already been processed.");

            var orderId = requestExport.OrderId;
            if (orderId == Guid.Empty)
                throw new InvalidOperationException("OrderId is missing.");

            var tempStockExports = await _tempExportRepo.GetByOrderIdAsync(orderId);
            if (tempStockExports == null || !tempStockExports.Any())
                throw new InvalidOperationException("No temporary stock exports found.");

            var groupedByProduct = tempStockExports
                .GroupBy(t => t.ProductId)
                .ToDictionary(
                    g => g.Key,
                    g => g.GroupBy(x => x.WarehouseId)
                          .ToDictionary(y => y.Key, y => y.ToList())
                );

            var transferRequestsToSave = new List<WarehouseTransferRequest>();
            var exportDetails = new List<ExportWarehouseReceiptDetail>();
            long? mainWarehouseId = null;

            foreach (var productGroup in groupedByProduct)
            {
                var productId = productGroup.Key;
                var warehouseGroups = productGroup.Value;
                // ✅ Xác định kho chính
                var maxWarehouse = warehouseGroups
                    .OrderByDescending(w => w.Value.Sum(x => x.Quantity))
                    .First();

                var mainId = maxWarehouse.Key;
                // Nếu lần đầu gặp sản phẩm → gán main warehouse
                if (mainWarehouseId == null)
                    mainWarehouseId = mainId;

                // ✅ Tạo chi tiết xuất kho cho kho chính
                foreach (var item in maxWarehouse.Value)
                {
                    var product = await _productRepository.GetByIdAsync(item.ProductId);
                    exportDetails.Add(new ExportWarehouseReceiptDetail
                    {
                        ProductId = item.ProductId,
                        ProductName = product?.ProductName ?? "Unknown",
                        BatchNumber = item.BatchNumber,
                        Quantity = (int)item.Quantity,
                        UnitPrice = item.UnitPrice,
                        TotalProductAmount = item.Quantity * item.UnitPrice,
                        ExpiryDate = item.ExpiryDate,
                        WarehouseProductId = item.WarehouseProductId,
                        BatchId = item.BatchId
                    });
                }

                // ✅ Các kho còn lại → tạo điều phối
                foreach (var other in warehouseGroups.Where(x => x.Key != mainId))
                {
                    transferRequestsToSave.Add(new WarehouseTransferRequest
                    {
                        SourceWarehouseId = other.Key,
                        DestinationWarehouseId = mainId,
                        Status = "Pending",
                        RequestDate = DateTime.UtcNow,
                        Notes = $"Transfer ProductId {productId} - Qty: {other.Value.Sum(x => x.Quantity)}",
                        TransferProducts = other.Value.Select(t => new WarehouseTransferProduct
                        {
                            ProductId = t.ProductId,
                            Quantity = (int)t.Quantity,
                            BatchId = t.BatchId
                        }).ToList()
                    });
                }
            }

            // ✅ Lưu ExportWarehouseReceipt cho kho chính
            var exportReceipt = new ExportWarehouseReceipt
            {
                DocumentNumber = $"PXK-CHINH-{DateTime.UtcNow.Ticks}",
                DocumentDate = DateTime.UtcNow,
                ExportDate = DateTime.UtcNow,
                ExportType = "PendingTransfer",
                Status = "Pending",
                WarehouseId = mainWarehouseId.Value,
                RequestExportId = requestExportId,
                ExportWarehouseReceiptDetails = exportDetails,
                TotalQuantity = exportDetails.Sum(x => x.Quantity),
                TotalAmount = exportDetails.Sum(x => x.TotalProductAmount)
            };

            await _exportReceiptRepo.AddRangeAsync(new List<ExportWarehouseReceipt> { exportReceipt }); // ✅ đúng
            await _transferRepo.AddRangeAsync(transferRequestsToSave);

            requestExport.Status = "Requested"; // hoặc Approved
            await _requestExportRepository.UpdateRequestExportAsync(requestExport);
            await _requestExportRepository.SaveChangesAsync();

            // Gửi thông báo đến kho chính
            await _hub.Clients.Group("3").SendAsync("ReceiveNotification", new
            {
                title = "Kho",
                message = "📦 Đơn xuất kho đã được duyệt. Vui lòng chuẩn bị xuất kho.",
                payload = $"RequestExportCode: {requestExport.RequestExportCode}"
            });

            return exportReceipt;
        }

    }

}
