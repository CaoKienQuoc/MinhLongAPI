using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessObject.DTO.Product;
using BusinessObject.DTO.Warehouse;
using BusinessObject.Models;
using Microsoft.AspNetCore.SignalR;
using Repo.IRepository;
using Repo.Repository;
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
        private readonly IRequestExportRepository _requestExportRepo;
        private readonly IOrderRepository _orderRepo;

        private readonly IHubContext<NotificationHub> _hub;

        public WarehouseExportService(
            ITemporaryWarehouseExportRepository tempExportRepo,
            IWarehouseTransferRepository transferRepo,
            IProductRepository productRepository,
            IHubContext<NotificationHub> hub,
            IRequestExportRepository requestExportRepository,
            IWarehouseExportRepository exportReceiptRepo,
            IRequestExportRepository requestExportRepo,
            IOrderRepository orderRepository)
        {
            _tempExportRepo = tempExportRepo;
            _transferRepo = transferRepo;
            _productRepository = productRepository;
            _hub = hub;
            _requestExportRepository = requestExportRepository;
            _exportReceiptRepo = exportReceiptRepo;
            _requestExportRepo = requestExportRepo;
            _orderRepo = orderRepository;
        }

        /*public async Task<ExportWarehouseReceipt> CreateExportReceiptForMainWarehouseAsync(int requestExportId, Guid currentUserId)
        {
            
            var requestExport = await _requestExportRepository.GetRequestExportByIdAsync(requestExportId);
            if (requestExport == null || requestExport.RequestExportDetails == null || !requestExport.RequestExportDetails.Any())
                throw new InvalidOperationException("RequestExport not found or invalid.");

            if (requestExport.Status == "Requested" || requestExport.Status == "Approved")
                throw new InvalidOperationException("This request has already been processed.");

            var orderId = requestExport.OrderId;
            if (orderId == Guid.Empty)
                throw new InvalidOperationException("OrderId is missing.");
            var order = await _orderRepo.GetOrderByIdAsync(orderId);

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
            order.Status = "WaitingDelivery"; // Cập nhật trạng thái đơn hàng
            
            await _requestExportRepository.UpdateRequestExportAsync(requestExport);
            await _orderRepo.UpdateOrderAsync(order);
            await _requestExportRepository.SaveChangesAsync();

            // Gửi thông báo đến kho chính
            await _hub.Clients.Group("3").SendAsync("ReceiveNotification", new
            {
                title = "Kho",
                message = "📦 Đơn xuất kho đã được duyệt. Vui lòng chuẩn bị xuất kho.",
                payload = $"RequestExportCode: {requestExport.RequestExportCode}"
            });

            return exportReceipt;
        }*/

        public async Task<ExportWarehouseReceipt> CreateExportReceiptForMainWarehouseAsync(int requestExportId, Guid currentUserId)
        {
            var requestExport = await _requestExportRepository.GetRequestExportByIdAsync(requestExportId)
                ?? throw new InvalidOperationException("RequestExport not found.");

            if (requestExport.Status == "Requested" || requestExport.Status == "Approved")
                throw new InvalidOperationException("This request has already been processed.");

            if (requestExport.RequestExportDetails == null || !requestExport.RequestExportDetails.Any())
                throw new InvalidOperationException("No product details in request.");

            var order = await _orderRepo.GetOrderByIdAsync(requestExport.OrderId);
            if (order == null) throw new InvalidOperationException("Order not found.");

            var tempStockExports = await _tempExportRepo.GetByOrderIdAsync(order.OrderId);
            if (tempStockExports == null || !tempStockExports.Any())
                throw new InvalidOperationException("No temporary stock exports found.");

            var exportDetails = new List<ExportWarehouseReceiptDetail>();
            var transferRequests = new List<WarehouseTransferRequest>();

            var warehouseIds = tempStockExports.Select(t => t.WarehouseId).Distinct().ToList();

            // ✅ Trường hợp 1: chỉ 1 kho duy nhất đủ tất cả
            if (warehouseIds.Count == 1)
            {
                var warehouseId = warehouseIds.First();
                foreach (var item in tempStockExports)
                {
                    var product = await _productRepository.GetByIdAsync(item.ProductId);
                    exportDetails.Add(new ExportWarehouseReceiptDetail
                    {
                        ProductId = item.ProductId,
                        ProductName = product?.ProductName ?? "Unknown",
                        BatchNumber = item.BatchNumber,
                        Quantity = (int)item.Quantity,
                        UnitPrice = item.UnitPrice,
                        TotalProductAmount = item.UnitPrice * item.Quantity,
                        ExpiryDate = item.ExpiryDate,
                        WarehouseProductId = item.WarehouseProductId,
                        BatchId = item.BatchId
                    });
                }

                var receipt = new ExportWarehouseReceipt
                {
                    DocumentNumber = $"PXK-{DateTime.UtcNow.Ticks}",
                    DocumentDate = DateTime.UtcNow,
                    ExportDate = DateTime.UtcNow,
                    ExportType = "AvailableExport",
                    Status = "Pending",
                    WarehouseId = warehouseId,
                    RequestExportId = requestExportId,
                    ExportWarehouseReceiptDetails = exportDetails,
                    TotalQuantity = exportDetails.Sum(x => x.Quantity),
                    TotalAmount = exportDetails.Sum(x => x.TotalProductAmount)
                };

                await _exportReceiptRepo.AddRangeAsync(new[] { receipt });
                await UpdateRequestAndOrderStatusAsync(requestExport, order);
                await SendWarehouseNotification(requestExport.RequestExportCode, "📦 Đơn hàng đủ tồn, xuất kho trực tiếp.");
                return receipt;
            }

            // ✅ Trường hợp 2: nhiều kho bị trừ → xác định kho chính
            var mainWarehouseId = tempStockExports
                .GroupBy(x => x.WarehouseId)
                .OrderByDescending(g => g.Sum(x => x.Quantity))
                .First().Key;

            foreach (var item in tempStockExports)
            {
                if (item.WarehouseId == mainWarehouseId)
                {
                    var product = await _productRepository.GetByIdAsync(item.ProductId);
                    exportDetails.Add(new ExportWarehouseReceiptDetail
                    {
                        ProductId = item.ProductId,
                        ProductName = product?.ProductName ?? "Unknown",
                        BatchNumber = item.BatchNumber,
                        Quantity = (int)item.Quantity,
                        UnitPrice = item.UnitPrice,
                        TotalProductAmount = item.UnitPrice * item.Quantity,
                        ExpiryDate = item.ExpiryDate,
                        WarehouseProductId = item.WarehouseProductId,
                        BatchId = item.BatchId
                    });
                }
                else
                {
                    var existing = transferRequests.FirstOrDefault(r => r.SourceWarehouseId == item.WarehouseId);
                    if (existing == null)
                    {
                        existing = new WarehouseTransferRequest
                        {
                            SourceWarehouseId = item.WarehouseId,
                            DestinationWarehouseId = mainWarehouseId,
                            RequestExportId = requestExportId,
                            WarehouseProductId = item.WarehouseProductId,
                            Status = "Pending",
                            RequestDate = DateTime.UtcNow,
                            Notes = $"Transfer for order {order.OrderCode}",
                            TransferProducts = new List<WarehouseTransferProduct>()
                        };
                        transferRequests.Add(existing);
                    }

                    existing.TransferProducts.Add(new WarehouseTransferProduct
                    {
                        ProductId = item.ProductId,
                        Quantity = (int)item.Quantity,
                        BatchId = item.BatchId
                    });
                }
            }

            var transferReceipt = new ExportWarehouseReceipt
            {
                DocumentNumber = $"PXK-{DateTime.UtcNow.Ticks}",
                DocumentDate = DateTime.UtcNow,
                ExportDate = DateTime.UtcNow,
                ExportType = "PendingTransfer",
                Status = "Pending",
                WarehouseId = mainWarehouseId,
                RequestExportId = requestExportId,
                ExportWarehouseReceiptDetails = exportDetails,
                TotalQuantity = exportDetails.Sum(x => x.Quantity),
                TotalAmount = exportDetails.Sum(x => x.TotalProductAmount)
            };

            await _exportReceiptRepo.AddRangeAsync(new[] { transferReceipt });
            await _transferRepo.AddRangeAsync(transferRequests);
            await UpdateRequestAndOrderStatusAsync(requestExport, order);
            await SendWarehouseNotification(requestExport.RequestExportCode, "📦 Đơn cần điều phối. Vui lòng chuẩn bị xuất kho.");
            return transferReceipt;
        }

        private async Task UpdateRequestAndOrderStatusAsync(RequestExport request, Order order)
        {
            request.Status = "Requested";
            order.Status = "WaitingDelivery";
            await _requestExportRepository.UpdateRequestExportAsync(request);
            await _orderRepo.UpdateOrderAsync(order);
            await _requestExportRepository.SaveChangesAsync();
        }

        private async Task SendWarehouseNotification(string code, string message)
        {
            await _hub.Clients.Group("3").SendAsync("ReceiveNotification", new
            {
                title = "Kho",
                message,
                payload = $"RequestExportCode: {code}"
            });
        }


        public async Task FinalizeExportSaleAsync(int exportReceiptId, Guid currentUserId)
        {
            
            // 1. Lấy phiếu xuất kho
            var receipt = await _exportReceiptRepo.GetByIdWithDetailsAsync(exportReceiptId);
            if (receipt == null)
                throw new InvalidOperationException("Không tìm thấy phiếu xuất kho.");

            if (receipt.Status != "Pending")
                throw new InvalidOperationException("Phiếu xuất kho đã được xử lý.");

            if (receipt.RequestExportId == null)
                throw new InvalidOperationException("Phiếu xuất không liên kết với đơn yêu cầu xuất kho.");

            // ❗ Nếu phiếu đang ở trạng thái "PendingTransfer", thì không cho xuất
            if (receipt.ExportType == "PendingTransfer")
            {
                throw new InvalidOperationException("Số lượng tồn kho không đủ, vui lòng điều phối hoặc nhập hàng thêm.");
            }
            var requestExport = await _requestExportRepo.GetRequestExportById(receipt.RequestExportId);
            // 2. Truy vết Order từ RequestExport
            var orderId = await _requestExportRepo.GetOrderIdByRequestExportIdAsync(receipt.RequestExportId);
            if (orderId == null || orderId == Guid.Empty)
                throw new InvalidOperationException("Không tìm thấy OrderId tương ứng từ đơn yêu cầu xuất kho.");

            var order = await _orderRepo.GetOrderByIdAsync(orderId.Value);

            // 3. Lấy danh sách bản ghi tạm và xoá tồn kho tạm
            var tempExports = await _tempExportRepo.GetByOrderIdAsync(orderId.Value);
            if (tempExports == null || !tempExports.Any())
                throw new InvalidOperationException("Không tìm thấy dữ liệu tạm để xoá tồn kho.");

            var tempIds = tempExports.Select(t => t.TemporaryStockExportId).ToList();
            await _tempExportRepo.DeleteByTemporaryExportIdsAsync(tempIds);

            // 4. Cập nhật phiếu xuất kho & đơn hàng
            receipt.Status = "Completed";
            receipt.ExportType = "ExportSale";
            order.Status = "Exported";
            requestExport.Status = "Approved";

            
            await _exportReceiptRepo.UpdateAsync(receipt);
            await _orderRepo.UpdateOrderAsync(order);
            await _requestExportRepo.UpdateExportAsync(requestExport);
            await _exportReceiptRepo.SaveChangesAsync();

            // 5. Gửi thông báo
            await _hub.Clients.Group("3").SendAsync("ReceiveNotification", new
            {
                title = "Xuất kho",
                message = $"✅ Phiếu xuất kho đã hoàn tất: {receipt.DocumentNumber}",
                payload = receipt.ExportWarehouseReceiptId
            });
        }

        public async Task<List<ExportWarehouseReceiptDTO>> GetAllExportsByUserAsync(Guid userId)
        {
            var receipts = await _exportReceiptRepo.GetAllByUserIdAsync(userId);
            var result = new List<ExportWarehouseReceiptDTO>();

            foreach (var r in receipts)
            {
                var dto = new ExportWarehouseReceiptDTO
                {
                    ExportWarehouseReceiptId = r.ExportWarehouseReceiptId,
                    DocumentNumber = r.DocumentNumber,
                    DocumentDate = r.DocumentDate,
                    ExportDate = r.ExportDate,
                    ExportType = r.ExportType,
                    TotalQuantity = r.TotalQuantity,
                    TotalAmount = r.TotalAmount,
                    Status = r.Status,
                    WarehouseId = r.WarehouseId,
                    RequestExportId = r.RequestExportId,
                    OrderCode = r.RequestExport.Order.OrderCode,
                    AgencyName = r.RequestExport.Order.RequestProduct.AgencyAccount.AgencyName,
                    Details = r.ExportWarehouseReceiptDetails.Select(d => new ExportWarehouseReceiptDetailDTO
                    {
                        WarehouseProductId = d.WarehouseProductId,
                        ProductId = d.ProductId,
                        ProductName = d.Product?.ProductName ?? "",
                        BatchNumber = d.BatchNumber,
                        Quantity = d.Quantity,
                        UnitPrice = d.UnitPrice,
                        TotalProductAmount = d.Quantity * d.UnitPrice,
                        ExpiryDate = d.ExpiryDate
                    }).ToList()
                };

                result.Add(dto);
            }

            return result;
        }

        public async Task<ExportWarehouseReceiptDTO?> GetExportByIdAsync(int exportReceiptId, Guid userId)
        {
            var r = await _exportReceiptRepo.GetByIdAndUserIdAsync(exportReceiptId, userId);
            if (r == null) return null;

            return new ExportWarehouseReceiptDTO
            {
                ExportWarehouseReceiptId = r.ExportWarehouseReceiptId,
                DocumentNumber = r.DocumentNumber,
                DocumentDate = r.DocumentDate,
                ExportDate = r.ExportDate,
                ExportType = r.ExportType,
                TotalQuantity = r.TotalQuantity,
                TotalAmount = r.TotalAmount,
                Status = r.Status,
                WarehouseId = r.WarehouseId,
                RequestExportId = r.RequestExportId,
                OrderCode = r.RequestExport.Order.OrderCode,
                AgencyName = r.RequestExport.Order.RequestProduct.AgencyAccount.AgencyName,
                Details = r.ExportWarehouseReceiptDetails.Select(d => new ExportWarehouseReceiptDetailDTO
                {
                    WarehouseProductId = d.WarehouseProductId,
                    ProductId = d.ProductId,
                    ProductName = d.Product?.ProductName ?? "",
                    BatchNumber = d.BatchNumber,
                    Quantity = d.Quantity,
                    UnitPrice = d.UnitPrice,
                    TotalProductAmount = d.Quantity * d.UnitPrice,
                    ExpiryDate = d.ExpiryDate
                }).ToList()
            };
        }

        public async Task UpdateExportFromCoordinationImportAsync(int requestExportId, List<BatchResponseDto> importedBatches)
        {
            var transferRequest = await _exportReceiptRepo.GetWarehouseTransferByRequestExportIdAsync(requestExportId);
            if (transferRequest == null)
                throw new Exception($"Không tìm thấy thông tin điều phối từ RequestExportId {requestExportId}");

            var mainExportReceipts = await _exportReceiptRepo.GetByRequestExportIdAsync(requestExportId);

            if (mainExportReceipts == null || !mainExportReceipts.Any())
                return;

            foreach (var exportReceipt in mainExportReceipts)
            {
                if (exportReceipt.ExportType != "PendingTransfer")
                    continue;

                var exportDetails = await _exportReceiptRepo.GetDetailsByReceiptIdAsync(exportReceipt.ExportWarehouseReceiptId);

                foreach (var batch in importedBatches)
                {
                    var matchingDetail = exportDetails.FirstOrDefault(x => x.ProductId == batch.ProductId);

                    if (matchingDetail != null)
                    {
                        matchingDetail.Quantity += batch.Quantity;
                        matchingDetail.TotalProductAmount = matchingDetail.Quantity * matchingDetail.UnitPrice;

                        await _exportReceiptRepo.UpdateDetailAsync(matchingDetail);
                    }
                    /*else
                    {
                        var newDetail = new ExportWarehouseReceiptDetail
                        {
                            ExportWarehouseReceiptId = exportReceipt.ExportWarehouseReceiptId,
                            ProductId = batch.ProductId,
                            ProductName = "",
                            BatchNumber = batch.BatchCode,
                            Quantity = batch.Quantity,
                            UnitPrice = batch.UnitCost,
                            TotalProductAmount = batch.TotalAmount,
                            ExpiryDate = batch.ExpiryDate,
                            BatchId = batch.BatchId ?? 0
                        };

                        await _exportReceiptRepo.AddDetailAsync(newDetail);
                    }*/
                }

                exportReceipt.TotalQuantity = exportDetails.Sum(x => x.Quantity);
                exportReceipt.TotalAmount = exportDetails.Sum(x => x.TotalProductAmount);
                exportReceipt.ExportType = "AvailableExport";

                await _exportReceiptRepo.UpdateReceiptAsync(exportReceipt);
            }

            await _exportReceiptRepo.SaveChangesAsync();
        }

    }

}
