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

        public async Task<List<ExportWarehouseReceipt>> CreateInternalTransferReceiptsAsync(int requestExportId, Guid currentUserId)
        {
            var requestExport = await _requestExportRepository.GetRequestExportByIdAsync(requestExportId);
            if (requestExport == null || requestExport.RequestExportDetails == null || !requestExport.RequestExportDetails.Any())
                throw new InvalidOperationException("RequestExport not found or invalid.");

            if (requestExport.Status == "Requested" || requestExport.Status == "Approved")
                throw new InvalidOperationException("This request has already been assigned.");

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

            var receiptsToSave = new List<ExportWarehouseReceipt>();
            var transferRequestsToSave = new List<WarehouseTransferRequest>();

            foreach (var productGroup in groupedByProduct)
            {
                var productId = productGroup.Key;
                var warehouseGroups = productGroup.Value;

                var mainWarehouse = warehouseGroups
                    .OrderByDescending(w => w.Value.Sum(x => x.Quantity))
                    .First().Key;

                var requestedQty = requestExport.RequestExportDetails
                    .Where(d => d.ProductId == productId)
                    .Sum(d => d.RequestedQuantity);

                // ✅ Tạo phiếu xuất từ kho chính (chờ điều phối)
                var mainReceipt = new ExportWarehouseReceipt
                {
                    DocumentNumber = $"PXK-CHINH-{DateTime.UtcNow.Ticks}",
                    DocumentDate = DateTime.UtcNow,
                    ExportDate = DateTime.UtcNow,
                    ExportType = "PendingTransfer", // chờ điều phối
                    Status = "Pending",
                    WarehouseId = mainWarehouse,
                    RequestExportId = requestExportId,
                    ExportWarehouseReceiptDetails = new List<ExportWarehouseReceiptDetail>()
                };

                foreach (var item in warehouseGroups[mainWarehouse])
                {
                    var product = await _productRepository.GetByIdAsync(item.ProductId);
                    mainReceipt.ExportWarehouseReceiptDetails.Add(new ExportWarehouseReceiptDetail
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

                mainReceipt.TotalQuantity = mainReceipt.ExportWarehouseReceiptDetails.Sum(x => x.Quantity);
                mainReceipt.TotalAmount = mainReceipt.ExportWarehouseReceiptDetails.Sum(x => x.TotalProductAmount);
                receiptsToSave.Add(mainReceipt);

                // ✅ Các kho còn lại → điều phối
                foreach (var kvp in warehouseGroups.Where(w => w.Key != mainWarehouse))
                {
                    var sourceWarehouseId = kvp.Key;
                    var transferBatches = kvp.Value;

                    var internalReceipt = new ExportWarehouseReceipt
                    {
                        DocumentNumber = $"PXK-DP-{DateTime.UtcNow.Ticks}-{sourceWarehouseId}",
                        DocumentDate = DateTime.UtcNow,
                        ExportDate = DateTime.UtcNow,
                        ExportType = "InternalTransfer",
                        Status = "Pending",
                        WarehouseId = sourceWarehouseId,
                        RequestExportId = requestExportId,
                        ExportWarehouseReceiptDetails = new List<ExportWarehouseReceiptDetail>()
                    };

                    foreach (var batch in transferBatches)
                    {
                        var product = await _productRepository.GetByIdAsync(batch.ProductId);
                        internalReceipt.ExportWarehouseReceiptDetails.Add(new ExportWarehouseReceiptDetail
                        {
                            ProductId = batch.ProductId,
                            ProductName = product?.ProductName ?? "Unknown",
                            BatchNumber = batch.BatchNumber,
                            Quantity = (int)batch.Quantity,
                            UnitPrice = batch.UnitPrice,
                            TotalProductAmount = batch.Quantity * batch.UnitPrice,
                            ExpiryDate = batch.ExpiryDate,
                            WarehouseProductId = batch.WarehouseProductId,
                            BatchId = batch.BatchId
                        });
                    }

                    internalReceipt.TotalQuantity = internalReceipt.ExportWarehouseReceiptDetails.Sum(x => x.Quantity);
                    internalReceipt.TotalAmount = internalReceipt.ExportWarehouseReceiptDetails.Sum(x => x.TotalProductAmount);
                    receiptsToSave.Add(internalReceipt);

                    // Optional: tạo đơn điều phối để theo dõi riêng
                    var transferRequest = new WarehouseTransferRequest
                    {
                        SourceWarehouseId = sourceWarehouseId,
                        DestinationWarehouseId = mainWarehouse,
                        Status = "Pending",
                        RequestDate = DateTime.UtcNow,
                        Notes = $"Transfer ProductId {productId} - Qty: {transferBatches.Sum(x => x.Quantity)}",
                        TransferProducts = transferBatches.Select(t => new WarehouseTransferProduct
                        {
                            ProductId = t.ProductId,
                            Quantity = (int)t.Quantity,
                            BatchId = t.BatchId
                        }).ToList()
                    };
                    transferRequestsToSave.Add(transferRequest);
                }
            }
            await _exportReceiptRepo.AddRangeAsync(receiptsToSave); // ✅ Thêm dòng này

            await _transferRepo.AddRangeAsync(transferRequestsToSave);

            requestExport.Status = "Requested";
            //requestExport.FulfillmentStatus = "WaitingForTransfer";

            await _requestExportRepository.UpdateRequestExportAsync(requestExport);
            await _requestExportRepository.SaveChangesAsync();

            await _hub.Clients.Group("3").SendAsync("ReceiveNotification", new
            {
                title = "Kho",
                message = "📦 Yêu cầu xuất kho đã được xử lý, chờ điều phối.",
                payload = $"RequestExportCode: {requestExport.RequestExportCode}"
            });

            return receiptsToSave;
        }
    }

}
