using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessObject.DTO.Product;
using BusinessObject.DTO.Warehouse;
using BusinessObject.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using QuestPDF.Helpers;
using Repo.IRepository;
using Repo.Repository;
using Services.Exceptions;
using Services.IService;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using static Org.BouncyCastle.Asn1.Cmp.Challenge;
using BusinessObject.DTO.Dashboard;

namespace Services.Service
{
    public class WarehouseReceiptService : IWarehouseReceiptService
    {
        private readonly IWarehouseReceiptRepository _receiptRepo;
        private readonly ITemporaryWarehouseExportRepository _tempExportRepo;
        private readonly IBatchRepository _batchRepo;
        private readonly IWarehouseRepository _warehouseRepository;
        private readonly IWarehouseExportRepository _warehouseExportRepository;
        private readonly IWarehouseTransferRepository _transferRepo;
        private readonly IProductRepository _productRepo;
        private readonly IWarehouseExportService _exportWarehouseService;

        public WarehouseReceiptService(
            IWarehouseReceiptRepository receiptRepo,
            ITemporaryWarehouseExportRepository tempExportRepo,
            IBatchRepository batchRepo,
            IWarehouseRepository warehouseRepository,
            IWarehouseExportRepository warehouseExportRepository,
            IWarehouseTransferRepository transferRepo,
            IProductRepository productRepo,
            IWarehouseExportService exportWarehouseService)
        {
            _receiptRepo = receiptRepo;
            _tempExportRepo = tempExportRepo;
            _batchRepo = batchRepo;
            _warehouseRepository = warehouseRepository;
            _warehouseExportRepository = warehouseExportRepository;
            _transferRepo = transferRepo;
            _productRepo = productRepo;
            _exportWarehouseService = exportWarehouseService;
        }

        public async Task<bool> CreateReceiptAsync(WarehouseReceiptRequest request, Guid currentUserId)
        {
            var random = new Random();
            var allowedTypes = new HashSet<string> { "ImportCoordination", "ImportProduction" };
            if (!allowedTypes.Contains(request.ImportType))
                throw new Exception("ImportType không hợp lệ. Chỉ chấp nhận ImportCoordination hoặc ImportProduction.");

            var warehouseUserId = await _warehouseRepository.GetUserIdByWarehouseIdAsync(request.WarehouseId);
            if (warehouseUserId != currentUserId)
                throw new UnauthorizedAccessException("Bạn không có quyền với kho này.");

            List<BatchResponseDto> processedBatches = new();
            int totalQuantity = 0;
            decimal totalPrice = 0;

            if (request.ImportType == "ImportCoordination")
            {
                if (request.OrderId == null || request.OrderId == Guid.Empty)
                    throw new Exception("OrderId là cần thiết đối với ImportCoordination.");

                var tempExports = await _tempExportRepo.GetByOrderIdAsync(request.OrderId.Value);
                if (tempExports == null || !tempExports.Any())
                    throw new Exception("Không có dữ liệu điều phối.");

                foreach (var temp in tempExports)
                {
                    string status = temp.Batch.ExpiryDate < DateTime.Now ? "EXPIRED" : temp.Batch.Status;

                    processedBatches.Add(new BatchResponseDto
                    {
                        BatchCode = temp.Batch.BatchCode,
                        ProductId = temp.ProductId,
                        Unit = temp.Batch?.Unit,
                        Quantity = (int)temp.Quantity,
                        UnitCost = temp.Batch.UnitCost,
                        TotalAmount = temp.Quantity * temp.Batch.UnitCost,
                        SellingPrice = temp.Batch.SellingPrice ?? 0, // giả định giá bán = nhập * 1.1
                        Status = status,
                        DateOfManufacture = temp.Batch?.DateOfManufacture ?? DateTime.Now,
                        ExpiryDate = temp.Batch.ExpiryDate
                    });

                    totalQuantity += (int)temp.Quantity;
                    totalPrice += temp.Quantity * temp.UnitPrice;
                }
            }
            else // ImportProduction
            {
                string batchCode = $"BA-{DateTime.UtcNow.Ticks}-{random.Next(1000, 9999)}";

                var groupedBatches = request.Batches
                        .GroupBy(b => new
                                {
                                b.ProductId,
                                DateOfManufacture = b.DateOfManufacture.Date, // ✅ sửa tên key tại đây
                            b.UnitCost,
                            b.Unit
                        })
                                .Select(g => new
                                {
                                    ProductId = g.Key.ProductId,
                                    DateOfManufacture = g.Key.DateOfManufacture, // ✅ giờ dùng được
                                    UnitCost = g.Key.UnitCost,
                                    Unit = g.Key.Unit,
                                    TotalQuantity = g.Sum(x => x.Quantity)
                                });


                /*foreach (var b in request.Batches)
                {
                    var product = await _productRepo.GetByIdAsync(b.ProductId);
                    if (product == null)
                        throw new Exception($"Không tìm thấy sản phẩm (ProductId: {b.ProductId})");

                    int defaultExpirationDays = product.DefaultExpiration ?? 720; // fallback nếu null
                    DateTime manufactureDate = b.DateOfManufacture;
                    DateTime expiryDate = manufactureDate.AddDays(defaultExpirationDays).AddDays(1); // ✅ cộng thêm 1 ngày

                    string status = expiryDate < DateTime.Now ? "EXPIRED" : "CALCULATING_PRICE";

                    

                    processedBatches.Add(new BatchResponseDto
                    {
                        BatchCode = batchCode,
                        ProductId = b.ProductId,
                        Unit = b.Unit,
                        Quantity = b.Quantity,
                        UnitCost = b.UnitCost,
                        TotalAmount = b.Quantity * b.UnitCost,
                        SellingPrice = 0,
                        Status = status,
                        DateOfManufacture = b.DateOfManufacture,
                        ExpiryDate = expiryDate,
                    });

                    totalQuantity += b.Quantity;
                    totalPrice += b.Quantity * b.UnitCost;
                }*/

                foreach (var group in groupedBatches)
                {
                    var product = await _productRepo.GetByIdAsync(group.ProductId);
                    if (product == null)
                        throw new Exception($"Không tìm thấy sản phẩm (ProductId: {group.ProductId})");

                    int defaultExpirationDays = product.DefaultExpiration ?? 720;
                    DateTime expiryDate = group.DateOfManufacture.AddDays(defaultExpirationDays).AddDays(1);

                    string status = expiryDate < DateTime.Now ? "EXPIRED" : "CALCULATING_PRICE";

                    processedBatches.Add(new BatchResponseDto
                    {
                        BatchCode = batchCode,
                        ProductId = group.ProductId,
                        Unit = group.Unit,
                        Quantity = group.TotalQuantity,
                        UnitCost = group.UnitCost,
                        TotalAmount = group.TotalQuantity * group.UnitCost,
                        SellingPrice = 0,
                        Status = status,
                        DateOfManufacture = group.DateOfManufacture,
                        ExpiryDate = expiryDate,
                    });

                    totalQuantity += group.TotalQuantity;
                    totalPrice += group.TotalQuantity * group.UnitCost;
                }

            }

            string batchesJson = JsonConvert.SerializeObject(processedBatches, Formatting.Indented);
            // ✅ Format mã chứng từ theo loại nhập
            string documentNumber;
            if (request.ImportType == "ImportCoordination")
            {
                documentNumber = $"IMP-TF-{DateTime.UtcNow.Ticks}-{random.Next(1000, 9999)}";
            }
            else if (request.ImportType == "ImportProduction")
            {
                documentNumber = $"IMP-NEW-{DateTime.UtcNow.Ticks}-{random.Next(1000, 9999)}";
            }
            else
            {
                throw new Exception("ImportType không hợp lệ. Chỉ chấp nhận ImportCoordination hoặc ImportProduction.");
            }

            // ✅ Bước 1: Lưu vào WarehouseReceipt
            var warehouseReceipt = new WarehouseReceipt
            {
                DocumentNumber = documentNumber,
                DocumentDate = DateTime.Now,
                WarehouseId = request.WarehouseId,
                ImportType = request.ImportType,
                Supplier = request.Supplier,
                DateImport = request.DateImport,
                TotalQuantity = totalQuantity,
                TotalPrice = totalPrice,
                BatchesJson = batchesJson,
                IsApproved = true
            };


            await _receiptRepo.AddAsync(warehouseReceipt);
            await _receiptRepo.SaveChangesAsync(); // Sau đó mới dùng dữ liệu receipt để lưu xuống ImportTransaction

            // ✅ Bước 2: Lưu vào ImportTransaction
            var importTransaction = new ImportTransaction
            {
                DocumentNumber = warehouseReceipt.DocumentNumber,
                DocumentDate = warehouseReceipt.DocumentDate,
                TypeImport = warehouseReceipt.ImportType,
                Note = $"Phiếu nhập từ WarehouseReceipt #{warehouseReceipt.WarehouseReceiptId}",
                WarehouseId = warehouseReceipt.WarehouseId,
                Supplier = warehouseReceipt.Supplier,
                DateImport = warehouseReceipt.DateImport
            };

            await _receiptRepo.AddImportTransactionAsync(importTransaction);
            await _receiptRepo.SaveChangesAsync();

            // ✅ Bước 3: Lưu vào ImportTransactionDetail và Batch
            foreach (var batch in processedBatches)
            {
                var detail = new ImportTransactionDetail
                {
                    ImportTransactionId = importTransaction.ImportTransactionId,
                    TotalQuantity = batch.Quantity,
                    TotalPrice = batch.TotalAmount,
                    Note = $"SP#{batch.ProductId} - Batch: {batch.BatchCode}"
                };
                await _receiptRepo.AddImportTransactionDetailAsync(detail);
                await _receiptRepo.SaveChangesAsync();

                var batchEntity = new Batch
                {
                    BatchCode = batch.BatchCode,
                    ProductId = batch.ProductId,
                    Quantity = batch.Quantity,
                    UnitCost = batch.UnitCost,
                    TotalAmount = batch.TotalAmount,
                    SellingPrice = batch.SellingPrice,
                    Unit = batch.Unit,
                    DateOfManufacture = batch.DateOfManufacture,
                    ExpiryDate = batch.ExpiryDate,
                    Status = batch.Status,
                    ImportTransactionDetailId = detail.ImportTransactionDetailId
                };
                await _batchRepo.AddAsync(batchEntity);
            }

            await _batchRepo.SaveChangesAsync();

            return true;
        }


        public async Task<List<WarehouseReceiptDTO>> GetAllReceiptsByUserAsync(Guid userId)
        {
            var receipts = await _receiptRepo.GetAllByUserIdAsync(userId);
            // ✅ Sắp xếp theo DateImport mới nhất trước
            receipts = receipts.OrderByDescending(r => r.DateImport).ToList();

            // ✅ Sắp xếp: DateImport mới nhất trước, trong cùng ngày thì đơn mới (theo Id) lên trước
            receipts = receipts
                .OrderByDescending(r => r.DateImport.Date) // Sắp xếp theo ngày nhập (không tính giờ)
                .ThenByDescending(r => r.DateImport)       // Nếu cần phân biệt giờ trong ngày
                .ThenByDescending(r => r.WarehouseReceiptId) // Nếu hai đơn trùng DateImport thì lấy đơn mới hơn
                .ToList();
            var result = new List<WarehouseReceiptDTO>();

            foreach (var receipt in receipts)
            {
                var warehouse = receipt.Warehouse ?? await _warehouseRepository.GetByIdAsync(receipt.WarehouseId);

                var batches = new List<BatchResponseDto>();
                if (!string.IsNullOrEmpty(receipt.BatchesJson))
                {
                    batches = JsonConvert.DeserializeObject<List<BatchResponseDto>>(receipt.BatchesJson) ?? new();
                }

                result.Add(new WarehouseReceiptDTO
                {
                    WarehouseReceiptId = receipt.WarehouseReceiptId,
                    DocumentNumber = receipt.DocumentNumber,
                    DocumentDate = DateTime.Now,
                    WarehouseId = receipt.WarehouseId,
                    WarehouseName = warehouse?.WarehouseName ?? "",
                    ImportType = receipt.ImportType,
                    Supplier = receipt.Supplier,
                    DateImport = receipt.DateImport,
                    TotalQuantity = receipt.TotalQuantity,
                    TotalPrice = receipt.TotalPrice,
                    Batches = batches,
                    IsApproved = receipt.IsApproved,
                });
            }

            return result;
        }


        public async Task<WarehouseReceiptDTO?> GetReceiptByIdAsync(long id, Guid userId)
        {
            var receipt = await _receiptRepo.GetByIdAndUserIdAsync(id, userId);
            if (receipt == null) return null;

            var warehouse = receipt.Warehouse ?? await _warehouseRepository.GetByIdAsync(receipt.WarehouseId);

            var batches = new List<BatchResponseDto>();
            if (!string.IsNullOrEmpty(receipt.BatchesJson))
            {
                batches = JsonConvert.DeserializeObject<List<BatchResponseDto>>(receipt.BatchesJson) ?? new();
            }

            return new WarehouseReceiptDTO
            {
                WarehouseReceiptId = receipt.WarehouseReceiptId,
                DocumentNumber = receipt.DocumentNumber,
                DocumentDate = receipt.DocumentDate,
                WarehouseId = receipt.WarehouseId,
                WarehouseName = warehouse?.WarehouseName ?? "",
                ImportType = receipt.ImportType,
                Supplier = receipt.Supplier,
                DateImport = receipt.DateImport,
                TotalQuantity = receipt.TotalQuantity,
                TotalPrice = receipt.TotalPrice,
                Batches = batches,
                IsApproved = receipt.IsApproved
            };
        }



        /*public async Task<bool> ImportApprovedTransfersAsync(long transferRequestId, Guid currentUserId)
        {
            var random = new Random();
            var transferRequests = await _transferRepo.GetApprovedTransfersByDestinationAsync(destinationWarehouseId);

            if (transferRequests == null || !transferRequests.Any())
                throw new Exception("Không có điều phối nào ở trạng thái Approved cho kho này.");
            

            foreach (var request in transferRequests)
            {
                var userId = await _warehouseRepository.GetUserIdByWarehouseIdAsync(request.DestinationWarehouseId);
                if (userId != currentUserId)
                    throw new UnauthorizedAccessException("Không có quyền thao tác với kho này.");

                // ✅ Lấy tên kho xuất từ SourceWarehouseId
                var sourceWarehouseName = await _warehouseRepository.GetWarehouseNameByIdAsync(request.SourceWarehouseId);


                // Xóa khai báo trùng lặp
                // ✅ Khai báo danh sách để lưu DTO và entity
                List<BatchResponseDto> batchDtos = new();
                List<Batch> batchEntities = new();

                // ✅ Tạo batches và batchDtos đồng thời
                foreach (var tp in request.TransferProducts)
                {
                    if (tp.Batch == null)
                        throw new Exception($"Không tìm thấy batch cho sản phẩm {tp.ProductId}");

                    string status = tp.Batch.ExpiryDate < DateTime.Now ? "EXPIRED" : tp.Batch.Status;

                    decimal totalAmount = tp.Quantity * tp.Batch.UnitCost;

                    var dto = new BatchResponseDto
                    {
                        BatchCode = tp.Batch.BatchCode,
                        ProductId = tp.ProductId,
                        Unit = tp.Batch.Unit,
                        Quantity = tp.Quantity,
                        UnitCost = tp.Batch.UnitCost,
                        TotalAmount = totalAmount,
                        SellingPrice = tp.Batch.SellingPrice ?? 0,
                        Status = status,
                        DateOfManufacture = tp.Batch.DateOfManufacture,
                        ExpiryDate = tp.Batch.ExpiryDate
                    };

                    var entity = new Batch
                    {
                        ProductId = tp.ProductId,
                        BatchCode = tp.Batch.BatchCode,
                        Quantity = tp.Quantity,
                        UnitCost = tp.Batch.UnitCost,
                        TotalAmount = totalAmount,
                        SellingPrice = tp.Batch.SellingPrice,
                        Unit = tp.Batch.Unit,
                        DateOfManufacture = tp.Batch.DateOfManufacture,
                        ExpiryDate = tp.Batch.ExpiryDate,
                        Status = status
                    };

                    batchDtos.Add(dto);
                    batchEntities.Add(entity);
                }

                var warehouseReceipt = new WarehouseReceipt
                {
                    DocumentNumber = $"IMP-TF-{DateTime.UtcNow.Ticks}-{random.Next(1000, 9999)}",
                    DocumentDate = DateTime.Now,
                    WarehouseId = request.DestinationWarehouseId,
                    ImportType = "ImportCoordination",
                    Supplier = sourceWarehouseName,
                    DateImport = DateTime.Now,
                    TotalQuantity = batchDtos.Sum(x => x.Quantity),
                    TotalPrice = batchDtos.Sum(x => x.TotalAmount),
                    IsApproved = true,
                    BatchesJson = JsonConvert.SerializeObject(batchDtos, Formatting.Indented)
                };

                await _receiptRepo.AddAsync(warehouseReceipt);
                await _receiptRepo.SaveChangesAsync();

                var importTransaction = new ImportTransaction
                {
                    DocumentNumber = warehouseReceipt.DocumentNumber,
                    DocumentDate = warehouseReceipt.DocumentDate,
                    TypeImport = warehouseReceipt.ImportType,
                    Note = $"Tạo từ phiếu điều phối #{request.Id}",
                    Supplier = warehouseReceipt.Supplier,
                    WarehouseId = warehouseReceipt.WarehouseId,
                    DateImport = warehouseReceipt.DateImport
                };

                await _receiptRepo.AddImportTransactionAsync(importTransaction);
                await _receiptRepo.SaveChangesAsync();

                for (int i = 0; i < batchEntities.Count; i++)
                {
                    var batchEntity = batchEntities[i];
                    var dto = batchDtos[i];

                    var detail = new ImportTransactionDetail
                    {
                        ImportTransactionId = importTransaction.ImportTransactionId,
                        TotalQuantity = dto.Quantity,
                        TotalPrice = dto.TotalAmount,
                        Note = $"SP #{dto.ProductId} - Batch: {dto.BatchCode}"
                    };
                    await _receiptRepo.AddImportTransactionDetailAsync(detail);
                    await _receiptRepo.SaveChangesAsync();

                    batchEntity.ImportTransactionDetailId = detail.ImportTransactionDetailId;
                    await _batchRepo.AddAsync(batchEntity);
                    await _batchRepo.SaveChangesAsync();

                    dto.BatchId = batchEntity.BatchId; // cập nhật lại BatchId trong DTO sau khi lưu
                }

                // ✅ Gọi service cập nhật lại đơn xuất kho tổng theo RequestExportId
                await _exportWarehouseService.UpdateExportFromCoordinationImportAsync(request.RequestExportId, batchDtos);

                
            }

            return true;
        }*/

        public async Task<bool> ImportApprovedTransferAsync(long transferRequestId, Guid currentUserId)
        {
            var random = new Random();

            // ✅ Truy vấn 1 phiếu điều phối Approved cụ thể
            var request = await _transferRepo.GetApprovedTransferByIdAsync(transferRequestId);
            if (request == null)
                throw new Exception("Không tìm thấy phiếu điều phối ở trạng thái Approved.");

            // ✅ Kiểm tra quyền user với kho đích
            var userId = await _warehouseRepository.GetUserIdByWarehouseIdAsync(request.DestinationWarehouseId);
            if (userId != currentUserId)
                throw new UnauthorizedAccessException("Không có quyền thao tác với kho này.");

            // ✅ Lấy tên kho xuất từ SourceWarehouseId
            var sourceWarehouseName = await _warehouseRepository.GetWarehouseNameByIdAsync(request.SourceWarehouseId);

            List<BatchResponseDto> batchDtos = new();
            List<Batch> batchEntities = new();

            foreach (var tp in request.TransferProducts)
            {
                if (tp.Batch == null)
                    throw new Exception($"Không tìm thấy batch cho sản phẩm {tp.ProductId}");

                string status = tp.Batch.ExpiryDate < DateTime.Now ? "EXPIRED" : tp.Batch.Status;
                decimal totalAmount = tp.Quantity * tp.Batch.UnitCost;

                var dto = new BatchResponseDto
                {
                    BatchCode = tp.Batch.BatchCode,
                    ProductId = tp.ProductId,
                    Unit = tp.Batch.Unit,
                    Quantity = tp.Quantity,
                    UnitCost = tp.Batch.UnitCost,
                    TotalAmount = totalAmount,
                    SellingPrice = tp.Batch.SellingPrice ?? 0,
                    Status = status,
                    DateOfManufacture = tp.Batch.DateOfManufacture,
                    ExpiryDate = tp.Batch.ExpiryDate
                };

                var entity = new Batch
                {
                    ProductId = tp.ProductId,
                    BatchCode = tp.Batch.BatchCode,
                    Quantity = tp.Quantity,
                    UnitCost = tp.Batch.UnitCost,
                    TotalAmount = totalAmount,
                    SellingPrice = tp.Batch.SellingPrice,
                    Unit = tp.Batch.Unit,
                    DateOfManufacture = tp.Batch.DateOfManufacture,
                    ExpiryDate = tp.Batch.ExpiryDate,
                    Status = status
                };

                batchDtos.Add(dto);
                batchEntities.Add(entity);
            }

            var warehouseReceipt = new WarehouseReceipt
            {
                DocumentNumber = $"IMP-TF-{DateTime.UtcNow.Ticks}-{random.Next(1000, 9999)}",
                DocumentDate = DateTime.Now,
                WarehouseId = request.DestinationWarehouseId,
                ImportType = "ImportCoordination",
                Supplier = sourceWarehouseName,
                DateImport = DateTime.Now,
                TotalQuantity = batchDtos.Sum(x => x.Quantity),
                TotalPrice = batchDtos.Sum(x => x.TotalAmount),
                IsApproved = true,
                BatchesJson = JsonConvert.SerializeObject(batchDtos, Formatting.Indented)
            };

            await _receiptRepo.AddAsync(warehouseReceipt);
            await _receiptRepo.SaveChangesAsync();

            var importTransaction = new ImportTransaction
            {
                DocumentNumber = warehouseReceipt.DocumentNumber,
                DocumentDate = warehouseReceipt.DocumentDate,
                TypeImport = warehouseReceipt.ImportType,
                Note = $"Tạo từ phiếu điều phối #{request.Id}",
                Supplier = warehouseReceipt.Supplier,
                WarehouseId = warehouseReceipt.WarehouseId,
                DateImport = warehouseReceipt.DateImport
            };

            await _receiptRepo.AddImportTransactionAsync(importTransaction);
            await _receiptRepo.SaveChangesAsync();

            for (int i = 0; i < batchEntities.Count; i++)
            {
                var batchEntity = batchEntities[i];
                var dto = batchDtos[i];

                var detail = new ImportTransactionDetail
                {
                    ImportTransactionId = importTransaction.ImportTransactionId,
                    TotalQuantity = dto.Quantity,
                    TotalPrice = dto.TotalAmount,
                    Note = $"SP #{dto.ProductId} - Batch: {dto.BatchCode}"
                };

                await _receiptRepo.AddImportTransactionDetailAsync(detail);
                await _receiptRepo.SaveChangesAsync();

                batchEntity.ImportTransactionDetailId = detail.ImportTransactionDetailId;
                await _batchRepo.AddAsync(batchEntity);
                await _batchRepo.SaveChangesAsync();

                dto.BatchId = batchEntity.BatchId;
            }

            await _exportWarehouseService.UpdateExportFromCoordinationImportAsync(request.RequestExportId, batchDtos);

            return true;
        }



        public async Task<int> GetTodayReceiptCountAsync(Guid userId)
        {
            var receipts = await _receiptRepo.GetAllByUserIdAsync(userId);
            var today = DateTime.Today;
            return receipts.Count(r => r.DocumentDate.Date == today);
        }

        public async Task<int> GetThisMonthReceiptCountAsync(Guid userId)
        {
            var receipts = await _receiptRepo.GetAllByUserIdAsync(userId);
            var now = DateTime.Now;
            return receipts.Count(r => r.DocumentDate.Month == now.Month && r.DocumentDate.Year == now.Year);
        }

        public async Task<int> GetTodayTotalQuantityAsync(Guid userId)
        {
            var receipts = await _receiptRepo.GetAllByUserIdAsync(userId);
            var today = DateTime.Today;
            return receipts
                .Where(r => r.DocumentDate.Date == today)
                .Sum(r => r.TotalQuantity);
        }

        public async Task<int> GetThisMonthTotalQuantityAsync(Guid userId)
        {
            var receipts = await _receiptRepo.GetAllByUserIdAsync(userId);
            var now = DateTime.Now;
            return receipts
                .Where(r => r.DocumentDate.Month == now.Month && r.DocumentDate.Year == now.Year)
                .Sum(r => r.TotalQuantity);
        }

        public async Task<decimal> GetTodayTotalPriceAsync(Guid userId)
        {
            var receipts = await _receiptRepo.GetAllByUserIdAsync(userId);
            var today = DateTime.Today;
            return receipts
                .Where(r => r.DocumentDate.Date == today)
                .Sum(r => r.TotalPrice);
        }

        public async Task<decimal> GetThisMonthTotalPriceAsync(Guid userId)
        {
            var receipts = await _receiptRepo.GetAllByUserIdAsync(userId);
            var now = DateTime.Now;
            return receipts
                .Where(r => r.DocumentDate.Month == now.Month && r.DocumentDate.Year == now.Year)
                .Sum(r => r.TotalPrice);
        }

        public async Task<byte[]> GenerateReceiptPdfAsync(long warehouseReceiptId, Guid userId)
        {
            var receipt = await _receiptRepo.GetByIdAndUserIdAsync(warehouseReceiptId, userId);
            if (receipt == null)
                throw new Exception("Không tìm thấy phiếu nhập kho hoặc bạn không có quyền truy cập.");

            var batches = new List<BusinessObject.DTO.Product.BatchResponseDto>();
            if (!string.IsNullOrEmpty(receipt.BatchesJson))
            {
                batches = JsonConvert.DeserializeObject<List<BusinessObject.DTO.Product.BatchResponseDto>>(receipt.BatchesJson) ?? new();
            }

            // 🧠 Map ProductId -> ProductName
            var productIds = batches.Select(b => b.ProductId).Distinct().ToList();
            var products = await _productRepo.GetListByIdsAsync(productIds);
            var productDict = products.ToDictionary(p => p.ProductId, p => p.ProductName);

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(30);

                    page.Header()
                        .Text("PHIẾU NHẬP KHO")
                        .SemiBold().FontSize(22).FontColor(Colors.Blue.Medium)
                        .AlignCenter();

                    page.Content().Column(column =>
                    {
                        column.Spacing(10);

                        // Thông tin phiếu
                        column.Item().Text($"Số chứng từ: {receipt.DocumentNumber}").FontSize(14);
                        column.Item().Text($"Ngày chứng từ: {receipt.DocumentDate:dd/MM/yyyy}").FontSize(14);
                        column.Item().Text($"Loại nhập: {receipt.ImportType}").FontSize(14);
                        column.Item().Text($"Nhà cung cấp: {receipt.Supplier ?? "N/A"}").FontSize(14);

                        column.Item().PaddingVertical(5).LineHorizontal(1);

                        // Bảng sản phẩm
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(4);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyle).Text("Tên sản phẩm").Bold();
                                header.Cell().Element(CellStyle).AlignCenter().Text("Số lượng").Bold();
                                header.Cell().Element(CellStyle).AlignRight().Text("Đơn giá").Bold();
                                header.Cell().Element(CellStyle).AlignRight().Text("Tổng tiền").Bold();
                            });

                            foreach (var batch in batches)
                            {
                                var productName = productDict.ContainsKey(batch.ProductId)
                                    ? productDict[batch.ProductId]
                                    : $"SP-{batch.ProductId}";

                                table.Cell().Element(CellStyle).Text(productName);
                                table.Cell().Element(CellStyle).AlignCenter().Text(batch.Quantity.ToString());
                                table.Cell().Element(CellStyle).AlignRight().Text(batch.UnitCost.ToString("N0"));
                                table.Cell().Element(CellStyle).AlignRight().Text(batch.TotalAmount.ToString("N0"));
                            }
                        });

                        column.Item().PaddingTop(5).LineHorizontal(1);

                        column.Item().AlignRight().Text($"Tổng cộng: {receipt.TotalPrice:N0} VNĐ")
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

        public async Task<List<object>> GetMonthlyReceiptStatsAllAsync()
        {
            return await _receiptRepo.GetMonthlyReceiptStatsAllAsync();
        }

        public async Task<WarehouseDashboardRangeDto> GetDashboardByDateRangeAsync(DateTime? startDate, DateTime? endDate)
        {
            var today = DateTime.Today;
            var firstDayOfMonth = new DateTime(today.Year, today.Month, 1);

            DateTime start = startDate ?? firstDayOfMonth;
            DateTime end = endDate ?? today;

            if (start > end)
                throw new ArgumentException("startDate phải nhỏ hơn hoặc bằng endDate");

            var receiptsInRange = await _receiptRepo.GetReceiptsByDateRangeAsync(start, end);

            var groupedByDate = receiptsInRange
                .GroupBy(r => r.DocumentDate.Date)
                .Select(g => new DailyWarehouseSummaryDto
                {
                    Date = g.Key,
                    Month = g.Key.Month,
                    Year = g.Key.Year,
                    TotalReceipts = g.Count(),
                    TotalQuantity = g.Sum(r => r.TotalQuantity),
                    TotalPrice = g.Sum(r => r.TotalPrice)
                })
                .OrderBy(d => d.Date)
                .ToList();

            var totalReceipts = groupedByDate.Sum(d => d.TotalReceipts);
            var totalQuantity = groupedByDate.Sum(d => d.TotalQuantity);
            var totalPrice = groupedByDate.Sum(d => d.TotalPrice);

            return new WarehouseDashboardRangeDto
            {
                DailySummaries = groupedByDate,
                TotalReceipts = totalReceipts,
                TotalQuantity = totalQuantity,
                TotalPrice = totalPrice
            };
        }



    }

}

