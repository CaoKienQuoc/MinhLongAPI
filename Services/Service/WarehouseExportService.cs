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
using BusinessObject.DTO.Dashboard;

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
        private readonly IUserRepository _userRepository;
        private readonly INotificationRepository _notificationRepository;
        private readonly IBatchRepository _batchRepository;
        private readonly IHubContext<NotificationHub> _hub;
        private readonly IRequestProductRepository _requestProductRepository;
        private readonly IEmailService _emailService;
        private readonly IWarehouseReceiptRepository _receiptRepo;
        private readonly IInventoryService _inventoryService;
        private readonly IDamagedStockRepository _damagedRepo;

        public WarehouseExportService(
            ITemporaryWarehouseExportRepository tempExportRepo,
            IWarehouseTransferRepository transferRepo,
            IProductRepository productRepository,
            IHubContext<NotificationHub> hub,
            IRequestExportRepository requestExportRepository,
            IWarehouseExportRepository exportReceiptRepo,
            IRequestExportRepository requestExportRepo,
            IOrderRepository orderRepository,
            IUserRepository userRepository,
            INotificationRepository notificationRepository,
            IBatchRepository batchRepository,
                IRequestProductRepository requestProductRepository,
                IEmailService emailService,
                IWarehouseReceiptRepository receiptRepo,
                IInventoryService inventoryService,
                IDamagedStockRepository damagedRepo)
        {
            _tempExportRepo = tempExportRepo;
            _transferRepo = transferRepo;
            _productRepository = productRepository;
            _hub = hub;
            _requestExportRepository = requestExportRepository;
            _exportReceiptRepo = exportReceiptRepo;
            _requestExportRepo = requestExportRepo;
            _orderRepo = orderRepository;
            _userRepository = userRepository;
            _notificationRepository = notificationRepository;
            _batchRepository = batchRepository;
            _requestProductRepository = requestProductRepository;
            _emailService = emailService;
            _receiptRepo = receiptRepo;
            _inventoryService = inventoryService;
            _damagedRepo = damagedRepo;
        }

        public DateTime GetVietnamTime()
        {
            // Lấy múi giờ Việt Nam (GMT+7)
            TimeZoneInfo vietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            DateTime vietnamTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vietnamTimeZone);
            return vietnamTime;
        }

        public async Task<ExportWarehouseReceipt> CreateExportReceiptForMainWarehouseAsync(int requestExportId, Guid currentUserId)
        {
            var random = new Random();
            var requestExport = await _requestExportRepository.GetRequestExportByIdAsync(requestExportId)
                ?? throw new InvalidOperationException("Không tìm thấy RequestExport.");

            if (requestExport.Status == "Requested" || requestExport.Status == "Approved" || requestExport.Status == "Canceled")
                throw new InvalidOperationException("Yêu cầu này hiện tại không thực hiện được.");

            if (requestExport.RequestExportDetails == null || !requestExport.RequestExportDetails.Any())
                throw new InvalidOperationException("Không có chi tiết sản phẩm trong yêu cầu.");

            var order = await _orderRepo.GetOrderByIdAsync(requestExport.OrderId);
            if (order == null) throw new InvalidOperationException("Không tìm thấy Order");

            var tempStockExports = await _tempExportRepo.GetByOrderIdAsync(order.OrderId);
            if (tempStockExports == null || !tempStockExports.Any())
                throw new InvalidOperationException("Không tìm thấy kho xuất tạm nào.");

            // ✅ Lấy giá bán cao nhất (SellingPrice) theo ProductId từ BatchRepository
            var productIds = tempStockExports.Select(t => t.ProductId).Distinct().ToList();
            //var batchPrices = await _batchRepository.GetHighestSellingPricesByProductIdsAsync(productIds);

            var exportDetails = new List<ExportWarehouseReceiptDetail>();
            var transferRequests = new List<WarehouseTransferRequest>();

            var warehouseIds = tempStockExports.Select(t => t.WarehouseId).Distinct().ToList();
            var discount = order.Discount; // Nếu Discount có thể null
            var finalPrice = order.FinalPrice; // Nếu Discount có thể null
            // ✅ Trường hợp 1: chỉ 1 kho duy nhất đủ tất cả
            if (warehouseIds.Count == 1)
            {
                var warehouseId = warehouseIds.First();
                foreach (var item in tempStockExports)
                {
                    var products = await _productRepository.GetListByIdsAsync(new List<long> { item.ProductId });
                    var product = products.FirstOrDefault()
                        ?? throw new InvalidOperationException($"Không tìm thấy sản phẩm với ID: {item.ProductId}");

                    var unitPrice = product.Price ?? 0;


                    exportDetails.Add(new ExportWarehouseReceiptDetail
                    {
                        ProductId = item.ProductId,
                        ProductName = product?.ProductName ?? "Unknown",
                        BatchNumber = item.BatchNumber,
                        Quantity = (int)item.Quantity,
                        UnitPrice = unitPrice,
                        TotalProductAmount = unitPrice * item.Quantity,
                        ExpiryDate = item.ExpiryDate,
                        WarehouseProductId = item.WarehouseProductId,
                        BatchId = item.BatchId
                    });
                }

                var receipt = new ExportWarehouseReceipt
                {
                    DocumentNumber = $"PXK-{GetVietnamTime().Ticks}-{random.Next(1000, 9999)}",
                    DocumentDate = GetVietnamTime(),
                    ExportDate = GetVietnamTime(),
                    ExportType = "AvailableExport",
                    Status = "Pending",
                    WarehouseId = warehouseId,
                    RequestExportId = requestExportId,
                    ExportWarehouseReceiptDetails = exportDetails,
                    TotalQuantity = exportDetails.Sum(x => x.Quantity),
                    TotalAmount = exportDetails.Sum(x => x.TotalProductAmount),
                    Discount = discount,
                    FinalPrice = finalPrice
                };

                await _exportReceiptRepo.AddRangeAsync(new[] { receipt });
                await UpdateRequestAndOrderStatusAsync(requestExport, order);


                // ✅ Gửi thông báo
                var message = $"📦 Đơn hàng {order.OrderCode} đã sẵn sàng xuất trực tiếp từ kho.";
                await SendWarehouseNotification(warehouseId, requestExport.RequestExportCode, message);


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
                    var products = await _productRepository.GetListByIdsAsync(new List<long> { item.ProductId });
                    var product = products.FirstOrDefault()
                        ?? throw new InvalidOperationException($"Không tìm thấy sản phẩm với ID: {item.ProductId}");

                    var unitPrice = product.Price ?? 0;

                    exportDetails.Add(new ExportWarehouseReceiptDetail
                    {
                        ProductId = item.ProductId,
                        ProductName = product?.ProductName ?? "Unknown",
                        BatchNumber = item.BatchNumber,
                        Quantity = (int)item.Quantity,
                        UnitPrice = unitPrice,
                        TotalProductAmount = unitPrice * item.Quantity,
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
                            RequestDate = GetVietnamTime(),
                            Notes = $"Điều Phối Đơn Hàng {order.OrderCode}",
                            TranferRequestCode = $"PDP-{GetVietnamTime().Ticks}-{random.Next(1000, 9999)}",
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
                DocumentNumber = $"PXK-{GetVietnamTime().Ticks}-{random.Next(1000, 9999)}",
                DocumentDate = GetVietnamTime(),
                ExportDate = GetVietnamTime(),
                ExportType = "PendingTransfer",
                Status = "Pending",
                WarehouseId = mainWarehouseId,
                RequestExportId = requestExportId,
                ExportWarehouseReceiptDetails = exportDetails,
                TotalQuantity = exportDetails.Sum(x => x.Quantity),
                TotalAmount = exportDetails.Sum(x => x.TotalProductAmount),
                Discount = discount,
                FinalPrice = finalPrice
            };

            await _exportReceiptRepo.AddRangeAsync(new[] { transferReceipt });
            await _transferRepo.AddRangeAsync(transferRequests);
            await UpdateRequestAndOrderStatusAsync(requestExport, order);


            var notifyMessage = $"📦 Đơn hàng {order.OrderCode} cần điều phối. Vui lòng chuẩn bị xuất kho.";
            await SendWarehouseNotification(mainWarehouseId, requestExport.RequestExportCode, notifyMessage);


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

        private async Task SendWarehouseNotification(long warehouseId, string code, string message)
        {
            // Gửi SignalR đến user của kho
            var userId = await _userRepository.GetUserIdByWarehouseIdAsync(warehouseId);
            if (userId == null) return;

            await _hub.Clients.User(userId.ToString()).SendAsync("ReceiveNotification", new
            {
                title = "Kho",
                message,
                payload = $"RequestExportCode: {code}"
            });

            var timeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            var vietnamNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);

            // Lưu vào DB Notification
            var notification = new Notification
            {
                UserId = userId.Value,
                Title = "Yêu cầu xuất kho",
                Message = message,
                Url = $"/warehouse/view-export",
                CreatedAt = vietnamNow
            };

            await _notificationRepository.AddAsync(notification);
            await _notificationRepository.SaveChangesAsync();
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

            if (receipt.Status == "Canceled")
                throw new InvalidOperationException("Phiếu xuất kho đã bị huỷ không thể xuất kho.");

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

            /*var tempIds = tempExports.Select(t => t.TemporaryStockExportId).ToList();
            await _tempExportRepo.DeleteByTemporaryExportIdsAsync(tempIds);*/

            foreach (var temp in tempExports)
            {
                temp.IsReverted = true;  // Đánh dấu đã xử lý/xóa tạm
            }
            await _tempExportRepo.UpdateRangeAsync(tempExports);

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


            var agencyUserId = order?.RequestProduct?.AgencyAccount?.UserId;

            if (agencyUserId != null)
            {
                var notifyMessage = $"✅ Đơn hàng {order.OrderCode} đã được xuất kho. Vui lòng chuẩn bị nhận hàng.";

                // Gửi SignalR đến đại lý
                await _hub.Clients.User(agencyUserId.ToString()).SendAsync("ReceiveNotification", new
                {
                    title = "Agency",
                    message = notifyMessage,
                    payload = receipt.ExportWarehouseReceiptId
                });

                var timeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
                var vietnamNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);

                // Ghi vào bảng thông báo
                var notification = new Notification
                {
                    UserId = agencyUserId.Value,
                    Title = "Thông báo xuất kho",
                    Message = notifyMessage,
                    Url = $"/agency/orders", // Cập nhật URL nếu cần
                    CreatedAt = vietnamNow
                };

                await _notificationRepository.AddAsync(notification);
                await _notificationRepository.SaveChangesAsync();
            }
        }

        public async Task<List<ExportWarehouseReceiptDTO>> GetAllExportsByUserAsync(Guid userId)
        {
            var receipts = await _exportReceiptRepo.GetAllByUserIdAsync(userId);
            receipts = receipts.OrderByDescending(r => r.ExportDate).ToList();
            var result = new List<ExportWarehouseReceiptDTO>();

            foreach (var receipt in receipts)
            {
                // ✅ Lấy danh sách RequestExportDetails cho từng receipt
                var requestExportDetails = receipt.RequestExport?.RequestExportDetails?.ToList() ?? new List<RequestExportDetail>();

                var dto = new ExportWarehouseReceiptDTO
                {
                    ExportWarehouseReceiptId = receipt.ExportWarehouseReceiptId,
                    DocumentNumber = receipt.DocumentNumber,
                    DocumentDate = receipt.DocumentDate,
                    ExportDate = receipt.ExportDate,
                    ExportType = receipt.ExportType,
                    TotalQuantity = receipt.TotalQuantity,
                    TotalAmount = receipt.TotalAmount,
                    Status = receipt.Status,
                    WarehouseId = receipt.WarehouseId,
                    WarehouseName = receipt.Warehouse.WarehouseName,
                    RequestExportId = receipt.RequestExportId,
                    OrderCode = receipt.RequestExport?.Order?.OrderCode ?? "",
                    AgencyName = receipt.RequestExport?.Order?.RequestProduct?.AgencyAccount?.AgencyName ?? "",
                    Discount = receipt.Discount,
                    FinalPrice = receipt.FinalPrice,
                    Reason = receipt.Reason,
                    Details = receipt.ExportWarehouseReceiptDetails.Select(detail =>
                    {
                        var requestedQuantity = requestExportDetails
                            .FirstOrDefault(x => x.ProductId == detail.ProductId)?.RequestedQuantity ?? 0;

                        return new ExportWarehouseReceiptDetailDTO
                        {
                            WarehouseProductId = detail.WarehouseProductId,
                            ProductId = detail.ProductId,
                            ProductName = detail.Product?.ProductName ?? "",
                            BatchNumber = detail.BatchNumber,
                            Quantity = detail.Quantity,
                            UnitPrice = detail.UnitPrice,
                            TotalProductAmount = detail.TotalProductAmount,
                            ExpiryDate = detail.ExpiryDate,
                            RequestedQuantity = requestedQuantity
                        };
                    }).ToList()
                };

                result.Add(dto);
            }

            return result;
        }


        public async Task<ExportWarehouseReceiptDTO?> GetExportByIdAsync(int exportReceiptId, Guid userId)
        {
            var receipt = await _exportReceiptRepo.GetByIdAndUserIdAsync(exportReceiptId, userId);
            if (receipt == null) return null;

            var requestExportDetails = receipt.RequestExport?.RequestExportDetails?.ToList() ?? new List<RequestExportDetail>();

            var dto = new ExportWarehouseReceiptDTO
            {
                ExportWarehouseReceiptId = receipt.ExportWarehouseReceiptId,
                DocumentNumber = receipt.DocumentNumber,
                DocumentDate = receipt.DocumentDate,
                ExportDate = receipt.ExportDate,
                ExportType = receipt.ExportType,
                TotalQuantity = receipt.TotalQuantity,
                TotalAmount = receipt.TotalAmount,
                Status = receipt.Status,
                WarehouseId = receipt.WarehouseId,
                WarehouseName = receipt.Warehouse.WarehouseName,
                RequestExportId = receipt.RequestExportId,
                OrderCode = receipt.RequestExport?.Order?.OrderCode ?? "",
                AgencyName = receipt.RequestExport?.Order?.RequestProduct?.AgencyAccount?.AgencyName ?? "",
                Reason = receipt.Reason,
                Details = receipt.ExportWarehouseReceiptDetails.Select(detail =>
                {
                    var requestedQuantity = requestExportDetails
                        .FirstOrDefault(x => x.ProductId == detail.ProductId)?.RequestedQuantity ?? 0;

                    return new ExportWarehouseReceiptDetailDTO
                    {
                        WarehouseProductId = detail.WarehouseProductId,
                        ProductId = detail.ProductId,
                        ProductName = detail.Product?.ProductName ?? "",
                        BatchNumber = detail.BatchNumber,
                        Quantity = detail.Quantity,
                        UnitPrice = detail.UnitPrice,
                        TotalProductAmount = detail.TotalProductAmount,
                        ExpiryDate = detail.ExpiryDate,
                        RequestedQuantity = requestedQuantity
                    };
                }).ToList()
            };

            return dto;
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
            var now = GetVietnamTime();
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
            var now = GetVietnamTime();
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
            var now = GetVietnamTime();
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

        public async Task<List<object>> GetMonthlyExportStatsAllAsync()
        {
            return await _exportReceiptRepo.GetMonthlyExportStatsAllAsync();
        }

        public async Task<ExportDashboardResponseDto> GetExportDashboardAsync(DateTime? fromDate, DateTime? toDate)
        {
            // Nếu không truyền ngày, lấy từ đầu tháng đến hôm nay (giờ VN)
            var vietnamNow = GetVietnamTime();
            var startDate = fromDate ?? new DateTime(vietnamNow.Year, vietnamNow.Month, 1);
            var endDate = toDate ?? vietnamNow.Date;

            var exports = await _exportReceiptRepo.GetAllAsync(); // hoặc repo method lấy tất cả phiếu xuất

            var filteredExports = exports
                .Where(e => e.DocumentDate.Date >= startDate && e.DocumentDate.Date <= endDate)
                .ToList();

            var groupedByDate = filteredExports
                .GroupBy(e => e.DocumentDate.Date)
                .Select(g => new DailyExportSummaryDto
                {
                    Date = g.Key,
                    Month = g.Key.Month,
                    Year = g.Key.Year,
                    TotalExports = g.Count(),
                    TotalQuantity = g.Sum(x => x.TotalQuantity),
                    TotalAmount = g.Sum(x => x.TotalAmount)
                })
                .OrderBy(d => d.Date)
                .ToList();

            return new ExportDashboardResponseDto
            {
                DailySummaries = groupedByDate,
                TotalExports = filteredExports.Count,
                TotalQuantity = filteredExports.Sum(x => x.TotalQuantity),
                TotalAmount = filteredExports.Sum(x => x.TotalAmount)
            };
        }

        public async Task<List<ProfitByMonthDto>> GetProfitStatsAsync(int? year = null, int? month = null)
        {
            var now = DateTime.Now;
            int queryYear = year ?? now.Year;

            // Nếu month null => tính cho cả năm, else tính cho tháng đó
            if (month.HasValue)
            {
                // Lấy dữ liệu nhập trong tháng, năm đó
                var importData = await _receiptRepo.GetAllByYearMonthAsync(queryYear, month.Value);
                var exportData = await _exportReceiptRepo.GetAllByYearMonthAsync(queryYear, month.Value);
                var damagedData = (await _damagedRepo.GetAllAsync())
            .Where(x => x.CreatedAt.Year == queryYear && x.CreatedAt.Month == month.Value)
            .ToList();

                decimal importCost = importData.Sum(r => r.TotalPrice);
                decimal exportRevenue = exportData.Sum(r => r.TotalAmount);
                decimal returnAmount = damagedData
            .Where(x => x.Status == "Return")
            .Sum(x => (x.Batch?.SellingPrice ?? 0) * x.Quantity);

                decimal cancelAmount = damagedData
            .Where(x => x.Status == "ExportCancel")
            .Sum(x => (x.Batch?.UnitCost ?? 0) * x.Quantity);

                decimal netRevenue = exportRevenue - returnAmount - cancelAmount;
                decimal profit = netRevenue - importCost;
                decimal profitPercent = netRevenue > 0 ? (profit / netRevenue) * 100 : 0;

                return new List<ProfitByMonthDto>
        {
            new ProfitByMonthDto
            {
                Year = queryYear,
                Month = month.Value,
                TotalImportCost = importCost,
                TotalExportRevenue = exportRevenue,
                ProfitAmount = profit,
                ProfitPercentage = profitPercent
            }
        };
            }
            else
            {
                // Tính cho toàn bộ năm, theo từng tháng
                var importData = await _receiptRepo.GetAllByYearAsync(queryYear);
                var exportData = await _exportReceiptRepo.GetAllByYearAsync(queryYear);
                var damagedData = (await _damagedRepo.GetAllAsync())
            .Where(x => x.CreatedAt.Year == queryYear)
            .ToList();

                var profitStats = new List<ProfitByMonthDto>();

                for (int m = 1; m <= 12; m++)
                {
                    decimal importCost = importData.Where(r => r.DocumentDate.Month == m).Sum(r => r.TotalPrice);
                    decimal exportRevenue = exportData.Where(r => r.DocumentDate.Month == m).Sum(r => r.TotalAmount);

                    var damagedInMonth = damagedData
                .Where(x => x.CreatedAt.Month == m)
                .ToList();

                    decimal returnAmount = damagedInMonth
                .Where(x => x.Status == "Return")
                .Sum(x => (x.Batch?.SellingPrice ?? 0) * x.Quantity);

                    decimal cancelAmount = damagedInMonth
                        .Where(x => x.Status == "ExportCancel")
                        .Sum(x => (x.Batch?.UnitCost ?? 0) * x.Quantity);

                    decimal netRevenue = exportRevenue - returnAmount - cancelAmount;
                    decimal profit = netRevenue - importCost;
                    decimal profitPercent = netRevenue > 0 ? (profit / netRevenue) * 100 : 0;

                    profitStats.Add(new ProfitByMonthDto
                    {
                        Year = queryYear,
                        Month = m,
                        TotalImportCost = importCost,
                        TotalExportRevenue = exportRevenue,
                        ProfitAmount = profit,
                        ProfitPercentage = profitPercent
                    });
                }

                return profitStats;
            }
        }

        public async Task<ProfitByYearDto> GetAnnualProfitAsync(int? year = null)
        {
            var now = DateTime.Now;
            int queryYear = year ?? now.Year;

            var importData = await _receiptRepo.GetAllByYearAsync(queryYear);
            var exportData = await _exportReceiptRepo.GetAllByYearAsync(queryYear);
            var damagedData = (await _damagedRepo.GetAllAsync())
        .Where(x => x.CreatedAt.Year == queryYear)
        .ToList();

            decimal totalImportCost = importData.Sum(r => r.TotalPrice);
            decimal totalExportRevenue = exportData.Sum(r => r.TotalAmount);
            decimal returnAmount = damagedData
        .Where(x => x.Status == "Return")
        .Sum(x => (x.Batch?.SellingPrice ?? 0) * x.Quantity);

            decimal cancelAmount = damagedData
                .Where(x => x.Status == "ExportCancel")
                .Sum(x => (x.Batch?.UnitCost ?? 0) * x.Quantity);

            decimal netRevenue = totalExportRevenue - returnAmount - cancelAmount;
            decimal profit = netRevenue - totalImportCost;
            decimal profitPercent = netRevenue > 0 ? (profit / netRevenue) * 100 : 0;

            return new ProfitByYearDto
            {
                Year = queryYear,
                TotalImportCost = totalImportCost,
                TotalExportRevenue = totalExportRevenue,
                ProfitAmount = profit,
                ProfitPercentage = profitPercent
            };
        }

        public async Task<ExportDashboardResponseDto> GetExportDashboardByUserWarehouseAsync(Guid userId, DateTime? fromDate, DateTime? toDate)
        {
            var vietnamNow = GetVietnamTime();
            var startDate = fromDate ?? new DateTime(vietnamNow.Year, vietnamNow.Month, 1);
            var endDate = toDate ?? vietnamNow.Date;

            // ✅ Lấy toàn bộ phiếu xuất từ kho thuộc user này
            var userExports = await _exportReceiptRepo.GetAllByUserIdAsync(userId);

            // ✅ Lọc theo khoảng thời gian
            var filteredExports = userExports
                .Where(e => e.DocumentDate.Date >= startDate && e.DocumentDate.Date <= endDate)
                .ToList();

            var groupedByDate = filteredExports
                .GroupBy(e => e.DocumentDate.Date)
                .Select(g => new DailyExportSummaryDto
                {
                    Date = g.Key,
                    Month = g.Key.Month,
                    Year = g.Key.Year,
                    TotalExports = g.Count(),
                    TotalQuantity = g.Sum(x => x.TotalQuantity),
                    TotalAmount = g.Sum(x => x.TotalAmount)
                })
                .OrderBy(d => d.Date)
                .ToList();

            return new ExportDashboardResponseDto
            {
                DailySummaries = groupedByDate,
                TotalExports = filteredExports.Count,
                TotalQuantity = filteredExports.Sum(x => x.TotalQuantity),
                TotalAmount = filteredExports.Sum(x => x.TotalAmount)
            };
        }

        public async Task<List<TopExportedProductDto>> GetTopExportedProductsAsync(Guid userId, int top)
        {
            var receipts = await _exportReceiptRepo.GetAllByUserIdAsync(userId);

            // Flatten all details
            var allDetails = receipts
                .SelectMany(r => r.ExportWarehouseReceiptDetails)
                .GroupBy(d => d.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    TotalQuantity = g.Sum(x => x.Quantity)
                })
                .OrderByDescending(x => x.TotalQuantity)
                .Take(top)
                .ToList();

            var productIds = allDetails.Select(x => x.ProductId).ToList();
            var products = await _productRepository.GetListByIdsAsync(productIds);

            var result = allDetails.Select(item =>
            {
                var product = products.FirstOrDefault(p => p.ProductId == item.ProductId);
                return new TopExportedProductDto
                {
                    ProductId = item.ProductId,
                    ProductCode = product?.ProductCode ?? $"SP-{item.ProductId}",
                    ProductName = product?.ProductName ?? "Không rõ",
                    TotalExportedQuantity = item.TotalQuantity
                };
            }).ToList();

            return result;
        }

        public async Task<List<ProfitByMonthDto>> GetProfitByUserWarehouseAsync(Guid userId, int? year = null, int? month = null)
        {
            int queryYear = year ?? DateTime.Now.Year;

            // Lấy warehouseId mà user sở hữu
            var warehouseIds = (await _userRepository.GetWarehousesByUserIdAsync(userId))
                                .Select(w => w.WarehouseId)
                                .ToList();

            if (!warehouseIds.Any())
                return new List<ProfitByMonthDto>();

            // Lấy phiếu nhập và xuất thuộc các kho này
            var allImports = (await _receiptRepo.GetAllByYearAsync(queryYear))
                                .Where(r => warehouseIds.Contains(r.WarehouseId))
                                .ToList();

            var allExports = (await _exportReceiptRepo.GetAllByYearAsync(queryYear))
                                .Where(r => warehouseIds.Contains(r.WarehouseId))
                                .ToList();
            var allDamaged = await _damagedRepo.GetAllByWarehousesAsync(warehouseIds, queryYear);


            if (month.HasValue)
            {
                var importInMonth = allImports.Where(r => r.DocumentDate.Month == month.Value).ToList();
                var exportInMonth = allExports.Where(r => r.DocumentDate.Month == month.Value).ToList();

                decimal returnAmount = allDamaged
                    .Where(x => x.Status == "Return" && x.CreatedAt.Month == month.Value)
                    .Sum(x => (x.Batch?.SellingPrice ?? 0) * x.Quantity);

                decimal cancelAmount = allDamaged
                    .Where(x => x.Status == "ExportCancel" && x.CreatedAt.Month == month.Value)
                    .Sum(x => (x.Batch?.UnitCost ?? 0) * x.Quantity);


                decimal importCost = importInMonth.Sum(r => r.TotalPrice);
                decimal exportRevenue = exportInMonth.Sum(r => r.TotalAmount);
                decimal netRevenue = exportRevenue - returnAmount - cancelAmount;
                decimal profit = netRevenue - importCost;
                decimal percent = netRevenue > 0 ? (profit / netRevenue) * 100 : 0;

                return new List<ProfitByMonthDto>
        {
            new ProfitByMonthDto
            {
                Year = queryYear,
                Month = month.Value,
                TotalImportCost = importCost,
                TotalExportRevenue = exportRevenue,
                ProfitAmount = profit,
                ProfitPercentage = percent
            }
        };
            }
            else
            {
                var result = new List<ProfitByMonthDto>();
                for (int m = 1; m <= 12; m++)
                {
                    var importCost = allImports.Where(r => r.DocumentDate.Month == m).Sum(r => r.TotalPrice);
                    var exportRevenue = allExports.Where(r => r.DocumentDate.Month == m).Sum(r => r.TotalAmount);
                    var damagedInMonth = allDamaged.Where(x => x.CreatedAt.Month == m).ToList();

                    decimal returnAmount = damagedInMonth
                        .Where(x => x.Status == "Return")
                        .Sum(x => (x.Batch?.SellingPrice ?? 0) * x.Quantity);

                    decimal cancelAmount = damagedInMonth
                        .Where(x => x.Status == "ExportCancel")
                        .Sum(x => (x.Batch?.UnitCost ?? 0) * x.Quantity);

                    decimal netRevenue = exportRevenue - returnAmount - cancelAmount;
                    decimal profit = netRevenue - importCost;
                    decimal percent = netRevenue > 0 ? (profit / netRevenue) * 100 : 0;

                    result.Add(new ProfitByMonthDto
                    {
                        Year = queryYear,
                        Month = m,
                        TotalImportCost = importCost,
                        TotalExportRevenue = exportRevenue,
                        ProfitAmount = profit,
                        ProfitPercentage = percent
                    });
                }

                return result;
            }
        }



        public async Task CancelRequestExportAsync(long warehouseRequestExportId, Guid? userId, string reason)
        {
            var warehouseRequestExport = await _exportReceiptRepo.GetExportWarehouseReceiptByIdAsync(warehouseRequestExportId);
            // 1. Lấy RequestExport
            var requestExport = await _requestExportRepository.GetRequestExportByIdAsync(warehouseRequestExport.RequestExportId)
                ?? throw new Exception("Không tìm thấy đơn xuất kho.");

            // 2. Lấy Order liên quan
            var order = await _orderRepo.GetOrderByIdAsync(requestExport.OrderId)
                ?? throw new Exception("Không tìm thấy đơn đặt hàng liên quan.");

            // 3. Lấy RequestProduct liên quan
            var requestProduct = await _requestProductRepository.GetRequestProductByRequestIdAsync(order.RequestId)
                ?? throw new Exception("Không tìm thấy yêu cầu sản phẩm liên quan.");

            // 4. Set status = "Canceled"
            requestExport.Status = "Canceled";
            requestExport.Reason = reason; // Lưu lý do hủy
            order.Status = "Canceled";
            order.Reason = reason; // Lưu lý do hủy
            requestProduct.RequestStatus = "Canceled";
            warehouseRequestExport.Status = "Canceled";
            warehouseRequestExport.Reason = reason; // Lưu lý do hủy

            await _inventoryService.RollbackStockForCancelledOrderAsync(order.OrderId);

            // 5. Update
            await _requestExportRepository.UpdateExportAsync(requestExport);
            await _orderRepo.UpdateOrderAsync(order);
            await _requestProductRepository.UpdateRequestAsync(requestProduct);
            await _exportReceiptRepo.UpdateReceiptAsync(warehouseRequestExport);
            await _exportReceiptRepo.SaveChangesAsync();

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
                order.FinalPrice
            );

            var salesUserId = order?.RequestProduct?.AgencyAccount?.ManagedByEmployee?.User?.UserId;

            if (salesUserId != null)
            {
                var agencyName = order?.RequestProduct?.AgencyAccount?.AgencyName;
                var notifyMessage = $"❌ Phiếu xuất cho đơn hàng {order.OrderCode} của {agencyName} đã bị huỷ.";

                // Gửi SignalR đến đại lý
                await _hub.Clients.User(salesUserId.ToString()).SendAsync("ReceiveNotification", new
                {
                    title = "huyguiSales",
                    message = notifyMessage,
                    payload = requestExport.RequestExportId
                });

                var timeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
                var vietnamNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);

                // Ghi vào bảng thông báo
                var notification = new Notification
                {
                    UserId = salesUserId.Value,
                    Title = "Phiếu xuất kho đơn hàng bị hủy",
                    Message = notifyMessage,
                    Url = $"/sales/export", // Cập nhật URL nếu cần
                    CreatedAt = vietnamNow
                };

                await _notificationRepository.AddAsync(notification);
                
            }

            if (agencyUserId != Guid.Empty)
            {
                string agencyMessage = $"❌ Phiếu xuất cho đơn hàng {order.OrderCode} của bạn đã bị huỷ. Vui lòng liên hệ sales để biết thêm chi tiết.";

                await _hub.Clients.User(agencyUserId.ToString()).SendAsync("ReceiveNotification", new
                {
                    title = "huyguiAgency",
                    message = agencyMessage,
                    payload = requestExport.RequestExportId
                });

                var timeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
                var vietnamNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);

                var agencyNotification = new Notification
                {
                    UserId = agencyUserId,
                    Title = "Phiếu xuất kho đơn hàng bị hủy",
                    Message = agencyMessage,
                    Url = $"/agency/orders",
                    CreatedAt = vietnamNow
                };

                await _notificationRepository.AddAsync(agencyNotification);
            }
            await _notificationRepository.SaveChangesAsync();
        }

    }

}
