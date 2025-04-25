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
using Repo.IRepository;
using Repo.Repository;
using Services.Exceptions;
using Services.IService;

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
            var allowedTypes = new HashSet<string> { "ImportCoordination", "ImportProduction" };
            if (!allowedTypes.Contains(request.ImportType))
                throw new Exception("ImportType is invalid! Only accepted: ImportCoordination, ImportProduction");

            var warehouseUserId = await _warehouseRepository.GetUserIdByWarehouseIdAsync(request.WarehouseId);
            if (warehouseUserId != currentUserId)
                throw new UnauthorizedAccessException("Bạn không có quyền với kho này.");

            List<BatchResponseDto> processedBatches = new();
            int totalQuantity = 0;
            decimal totalPrice = 0;

            if (request.ImportType == "ImportCoordination")
            {
                if (request.OrderId == null || request.OrderId == Guid.Empty)
                    throw new Exception("OrderId is required for ImportCoordination.");

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
                foreach (var b in request.Batches)
                {
                    var product = await _productRepo.GetByIdAsync(b.ProductId);
                    if (product == null)
                        throw new Exception($"Không tìm thấy sản phẩm (ProductId: {b.ProductId})");

                    int defaultExpirationDays = product.DefaultExpiration ?? 720; // fallback nếu null
                    DateTime manufactureDate = b.DateOfManufacture;
                    DateTime expiryDate = manufactureDate.AddDays(defaultExpirationDays);

                    string status = expiryDate < DateTime.Now ? "EXPIRED" : "CALCULATING_PRICE";

                    processedBatches.Add(new BatchResponseDto
                    {
                        BatchCode = $"BA{DateTime.Now:yyyyMMddHHmmss}",
                        ProductId = b.ProductId,
                        Unit = b.Unit,
                        Quantity = b.Quantity,
                        UnitCost = b.UnitCost,
                        TotalAmount = b.Quantity * b.UnitCost,
                        SellingPrice = 0,
                        Status = status,
                        DateOfManufacture = b.DateOfManufacture,
                        ExpiryDate = expiryDate
                    });

                    totalQuantity += b.Quantity;
                    totalPrice += b.Quantity * b.UnitCost;
                }
            }

            string batchesJson = JsonConvert.SerializeObject(processedBatches, Formatting.Indented);
            // ✅ Format mã chứng từ theo loại nhập
            string documentNumber;
            if (request.ImportType == "ImportCoordination")
            {
                documentNumber = $"IMP-TF-{DateTime.Now:yyyyMMddHHmmss}";
            }
            else if (request.ImportType == "ImportProduction")
            {
                documentNumber = $"IMP-NEW-{DateTime.Now:yyyyMMddHHmmss}";
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
                DateImport = DateTime.Now,
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

        /*public async Task<bool> ImportApprovedTransfersAsync(long destinationWarehouseId, Guid currentUserId)
        {
            var transferRequests = await _transferRepo.GetApprovedTransfersByDestinationAsync(destinationWarehouseId);

            if (transferRequests == null || !transferRequests.Any())
                throw new Exception("Không có điều phối nào ở trạng thái Approved cho kho này.");

            foreach (var request in transferRequests)
            {
                var userId = await _warehouseRepository.GetUserIdByWarehouseIdAsync(request.DestinationWarehouseId);
                if (userId != currentUserId)
                    throw new UnauthorizedAccessException("Không có quyền thao tác với kho này.");

                // ✅ Lấy batches từ transfer
                var batches = request.TransferProducts.Select(tp =>
                {
                    if (tp.Batch == null)
                        throw new Exception($"Không tìm thấy batch cho sản phẩm {tp.ProductId}");

                    decimal totalAmount = tp.Quantity * tp.Batch.UnitCost;

                    return new BatchResponseDto
                    {
                        BatchCode = tp.Batch.BatchCode,
                        ProductId = tp.ProductId,
                        Unit = tp.Batch.Unit,
                        Quantity = tp.Quantity,
                        UnitCost = tp.Batch.UnitCost,
                        TotalAmount = totalAmount,
                        SellingPrice = tp.Batch.SellingPrice ?? 0,
                        Status = tp.Batch.Status,
                        DateOfManufacture = tp.Batch.DateOfManufacture,
                        ExpiryDate = tp.Batch.ExpiryDate
                    };
                }).ToList();

                decimal totalPrice = batches.Sum(x => x.TotalAmount);
                int totalQuantity = batches.Sum(x => x.Quantity);

                // ✅ Bước 1: Lưu vào WarehouseReceipt
                var warehouseReceipt = new WarehouseReceipt
                {
                    DocumentNumber = $"IMP-TF-{DateTime.Now:yyyyMMddHHmmss}",
                    DocumentDate = DateTime.Now,
                    WarehouseId = request.DestinationWarehouseId,
                    ImportType = "ImportCoordination",
                    Supplier = $"Kho #{request.SourceWarehouseId}",
                    DateImport = DateTime.Now,
                    TotalQuantity = totalQuantity,
                    TotalPrice = totalPrice,
                    IsApproved = true,
                    BatchesJson = JsonConvert.SerializeObject(batches, Formatting.Indented)
                };

                await _receiptRepo.AddAsync(warehouseReceipt);
                await _receiptRepo.SaveChangesAsync(); // Lấy xong receipt mới tạo giao dịch

                // ✅ Bước 2: Lưu ImportTransaction
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

                // ✅ Bước 3: Lưu ImportTransactionDetail + Batch
                foreach (var b in batches)
                {
                    var detail = new ImportTransactionDetail
                    {
                        ImportTransactionId = importTransaction.ImportTransactionId,
                        TotalQuantity = b.Quantity,
                        TotalPrice = b.TotalAmount,
                        Note = $"SP #{b.ProductId} - Batch: {b.BatchCode}"
                    };
                    await _receiptRepo.AddImportTransactionDetailAsync(detail);
                    await _receiptRepo.SaveChangesAsync();

                    var batchEntity = new Batch
                    {
                        ProductId = b.ProductId,
                        BatchCode = b.BatchCode,
                        Quantity = b.Quantity,
                        UnitCost = b.UnitCost,
                        TotalAmount = b.TotalAmount,
                        SellingPrice = b.SellingPrice,
                        Unit = b.Unit,
                        DateOfManufacture = b.DateOfManufacture,
                        ExpiryDate = b.ExpiryDate,
                        Status = b.Status,
                        ImportTransactionDetailId = detail.ImportTransactionDetailId
                    };
                    await _batchRepo.AddAsync(batchEntity);
                }
                await _batchRepo.SaveChangesAsync();

                // ✅ Gọi service cập nhật lại đơn xuất kho tổng theo RequestExportId
                await _exportWarehouseService.UpdateExportFromCoordinationImportAsync(request.RequestExportId, batches);
            }

            return true;
        }*/

        public async Task<bool> ImportApprovedTransfersAsync(long destinationWarehouseId, Guid currentUserId)
        {
            var transferRequests = await _transferRepo.GetApprovedTransfersByDestinationAsync(destinationWarehouseId);

            if (transferRequests == null || !transferRequests.Any())
                throw new Exception("Không có điều phối nào ở trạng thái Approved cho kho này.");
            

            foreach (var request in transferRequests)
            {
                var userId = await _warehouseRepository.GetUserIdByWarehouseIdAsync(request.DestinationWarehouseId);
                if (userId != currentUserId)
                    throw new UnauthorizedAccessException("Không có quyền thao tác với kho này.");

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
                    DocumentNumber = $"IMP-TF-{DateTime.Now:yyyyMMddHHmmss}",
                    DocumentDate = DateTime.Now,
                    WarehouseId = request.DestinationWarehouseId,
                    ImportType = "ImportCoordination",
                    Supplier = $"Kho #{request.SourceWarehouseId}",
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


    }

}

