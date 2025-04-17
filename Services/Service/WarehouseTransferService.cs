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
    public class WarehouseTransferService : IWarehouseTransferService
    {
        private readonly ITemporaryWarehouseExportRepository _tempExportRepo;
        private readonly IWarehouseTransferRepository _transferRepo;
        private readonly IProductRepository _productRepository;
        private readonly IWarehouseExportRepository _exportReceiptRepo;

        private readonly IHubContext<NotificationHub> _hub;

        public WarehouseTransferService(
            ITemporaryWarehouseExportRepository tempExportRepo,
            IWarehouseTransferRepository transferRepo,
            IProductRepository productRepository,
            IHubContext<NotificationHub> hub,
            IWarehouseExportRepository exportReceiptRepo)
        {
            _tempExportRepo = tempExportRepo;
            _transferRepo = transferRepo;
            _productRepository = productRepository;
            _hub = hub;
            _exportReceiptRepo = exportReceiptRepo;
        }
        public async Task<ExportWarehouseReceipt> ApproveTransferRequestAndCreateReceiptAsync(int transferRequestId)
        {
            var transferRequest = await _transferRepo.GetByIdAsync(transferRequestId);

            if (transferRequest == null || transferRequest.TransferProducts == null || !transferRequest.TransferProducts.Any())
                throw new InvalidOperationException("Transfer request not found or invalid.");

            if (transferRequest.Status == "Approved")
                throw new InvalidOperationException("Transfer request has already been approved.");

            // ✅ Tạo phiếu xuất kho điều phối từ kho phụ
            var exportReceipt = new ExportWarehouseReceipt
            {
                DocumentNumber = $"PXK-DP-{DateTime.UtcNow.Ticks}",
                DocumentDate = DateTime.UtcNow,
                ExportDate = DateTime.UtcNow,
                ExportType = "ExportCoordination",
                Status = "Approved",
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
                    BatchNumber = tempExport.BatchNumber
                });
            }


            exportReceipt.TotalQuantity = exportReceipt.ExportWarehouseReceiptDetails.Sum(x => x.Quantity);
            exportReceipt.TotalAmount = exportReceipt.ExportWarehouseReceiptDetails.Sum(x => x.TotalProductAmount);

            await _exportReceiptRepo.AddRangeAsync(new List<ExportWarehouseReceipt> { exportReceipt });

            // ✅ Cập nhật trạng thái điều phối
            transferRequest.Status = "Approved";
            await _transferRepo.UpdateAsync(transferRequest);
            await _transferRepo.SaveChangesAsync();

            // Gửi thông báo (nếu cần)
            await _hub.Clients.Group("3").SendAsync("ReceiveNotification", new
            {
                title = "Kho phụ",
                message = "📦 Phiếu điều phối đã được duyệt và xuất kho.",
                payload = $"TransferRequestId: {transferRequestId}"
            });

            return exportReceipt;
        }
    }
}
