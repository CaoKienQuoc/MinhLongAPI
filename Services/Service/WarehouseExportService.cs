using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessObject.DTO.Product;
using BusinessObject.DTO.Warehouse;
using BusinessObject.Models;
using Microsoft.AspNetCore.SignalR;
using QuestPDF.Helpers;
using Repo.IRepository;
using Repo.Repository;
using Services.IService;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using static Org.BouncyCastle.Asn1.Cmp.Challenge;

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

        public async Task<ExportWarehouseReceipt> CreateExportReceiptForMainWarehouseAsync(int requestExportId, Guid currentUserId)
        {
            var random = new Random();
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
                    DocumentNumber = $"PXK-{DateTime.UtcNow.Ticks}-{random.Next(1000, 9999)}",
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
                DocumentNumber = $"PXK-{DateTime.UtcNow.Ticks}-{random.Next(1000, 9999)}",
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
                transferRequest.Status = "Completed";

                await _exportReceiptRepo.UpdateReceiptAsync(exportReceipt);
            }

            await _exportReceiptRepo.SaveChangesAsync();
        }


        public async Task<int> GetTodayExportCountAsync(Guid userId)
        {
            var receipts = await _exportReceiptRepo.GetAllByUserIdAsync(userId);
            return receipts.Count(r => r.DocumentDate.Date == DateTime.Today);
        }

        public async Task<int> GetThisMonthExportCountAsync(Guid userId)
        {
            var receipts = await _exportReceiptRepo.GetAllByUserIdAsync(userId);
            var now = DateTime.Now;
            return receipts.Count(r => r.DocumentDate.Month == now.Month && r.DocumentDate.Year == now.Year);
        }

        public async Task<int> GetTodayExportQuantityAsync(Guid userId)
        {
            var receipts = await _exportReceiptRepo.GetAllByUserIdAsync(userId);
            return receipts
                .Where(r => r.DocumentDate.Date == DateTime.Today)
                .Sum(r => r.TotalQuantity);
        }

        public async Task<int> GetThisMonthExportQuantityAsync(Guid userId)
        {
            var now = DateTime.Now;
            var receipts = await _exportReceiptRepo.GetAllByUserIdAsync(userId);
            return receipts
                .Where(r => r.DocumentDate.Month == now.Month && r.DocumentDate.Year == now.Year)
                .Sum(r => r.TotalQuantity);
        }

        public async Task<decimal> GetTodayExportValueAsync(Guid userId)
        {
            var receipts = await _exportReceiptRepo.GetAllByUserIdAsync(userId);
            return receipts
                .Where(r => r.DocumentDate.Date == DateTime.Today)
                .Sum(r => r.TotalAmount);
        }

        public async Task<decimal> GetThisMonthExportValueAsync(Guid userId)
        {
            var now = DateTime.Now;
            var receipts = await _exportReceiptRepo.GetAllByUserIdAsync(userId);
            return receipts
                .Where(r => r.DocumentDate.Month == now.Month && r.DocumentDate.Year == now.Year)
                .Sum(r => r.TotalAmount);
        }


        public async Task<byte[]> GenerateExportReceiptPdfAsync(int exportReceiptId, Guid userId)
        {
            var receipt = await _exportReceiptRepo.GetByIdAndUserIdAsync(exportReceiptId, userId);
            if (receipt == null)
                throw new Exception("Không tìm thấy phiếu xuất kho hoặc bạn không có quyền truy cập.");

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(30);

                    page.Header()
                        .Text("PHIẾU XUẤT KHO")
                        .SemiBold().FontSize(22).FontColor(Colors.Blue.Medium)
                        .AlignCenter();

                    page.Content().Column(column =>
                    {
                        column.Spacing(10);

                        // Thông tin phiếu
                        column.Item().Text($"Số phiếu: {receipt.DocumentNumber}").FontSize(14);
                        column.Item().Text($"Ngày lập phiếu: {receipt.DocumentDate:dd/MM/yyyy}").FontSize(14);
                        column.Item().Text($"Ngày xuất: {receipt.ExportDate:dd/MM/yyyy}").FontSize(14);
                        column.Item().Text($"Kho xuất: {receipt.Warehouse?.WarehouseName ?? "N/A"}").FontSize(14);

                        column.Item().PaddingVertical(5).LineHorizontal(1);

                        // Bảng chi tiết sản phẩm
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(4); // Tên sản phẩm
                                columns.RelativeColumn(1); // Số lượng
                                columns.RelativeColumn(2); // Đơn giá
                                columns.RelativeColumn(2); // Thành tiền
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyle).Text("Tên sản phẩm").Bold();
                                header.Cell().Element(CellStyle).AlignCenter().Text("Số lượng").Bold();
                                header.Cell().Element(CellStyle).AlignRight().Text("Đơn giá").Bold();
                                header.Cell().Element(CellStyle).AlignRight().Text("Thành tiền").Bold();
                            });

                            foreach (var d in receipt.ExportWarehouseReceiptDetails)
                            {
                                table.Cell().Element(CellStyle).Text(d.ProductName);
                                table.Cell().Element(CellStyle).AlignCenter().Text(d.Quantity.ToString());
                                table.Cell().Element(CellStyle).AlignRight().Text(d.UnitPrice.ToString("N0"));
                                table.Cell().Element(CellStyle).AlignRight().Text(d.TotalProductAmount.ToString("N0"));
                            }
                        });

                        column.Item().PaddingTop(5).LineHorizontal(1);

                        column.Item().AlignRight().Text($"Tổng cộng: {receipt.TotalAmount:N0} VNĐ")
                            .FontSize(16).Bold().FontColor(Colors.Red.Medium);
                    });

                    page.Footer()
                        .AlignCenter()
                        .Text("Cảm ơn quý khách!")
                        .FontSize(10).FontColor(Colors.Grey.Medium);
                });
            });

            return document.GeneratePdf();
        }

        private static IContainer CellStyle(IContainer container)
        {
            return container
                .PaddingVertical(5)
                .PaddingHorizontal(2)
                .BorderBottom(1)
                .BorderColor(Colors.Grey.Lighten2);
        }

    }

}
