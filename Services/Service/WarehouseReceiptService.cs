using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessObject.DTO.Product;
using BusinessObject.DTO.Warehouse;
using BusinessObject.Models;
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

        public WarehouseReceiptService(
            IWarehouseReceiptRepository receiptRepo,
            ITemporaryWarehouseExportRepository tempExportRepo,
            IBatchRepository batchRepo,
            IWarehouseRepository warehouseRepository,
            IWarehouseExportRepository warehouseExportRepository)
        {
            _receiptRepo = receiptRepo;
            _tempExportRepo = tempExportRepo;
            _batchRepo = batchRepo;
            _warehouseRepository = warehouseRepository;
            _warehouseExportRepository = warehouseExportRepository;
        }

        public async Task<bool> CreateReceiptAsync(WarehouseReceiptRequest request, Guid currentUserId)
        {
            var allowedTypes = new HashSet<string> { "ImportCoordination", "ImportProduction" };

            if (!allowedTypes.Contains(request.ImportType))
                throw new Exception("ImportType is invalid! Only accepted: ImportCoordination, ImportProduction");

            // ✅ Kiểm tra quyền sở hữu kho
            var warehouseUserId = await _warehouseRepository.GetUserIdByWarehouseIdAsync(request.WarehouseId);
            if (warehouseUserId != currentUserId)
                throw new BadRequestException("Kho này không phải kho của bạn! Bạn không có quyền gì ở kho này.");

            string datePart = DateTime.Now.ToString("yyyyMMdd");
            int batchCountToday = await _batchRepo.CountBatchesByDateAsync(DateTime.Now);
            string batchCode = $"BA{datePart}-{(batchCountToday + 1):D3}";

            List<BatchResponseDto> processedBatches = new();
            int totalQuantity = 0;
            decimal totalPrice = 0;

            if (request.ImportType == "ImportCoordination")
            {
                // ✅ Cần OrderId để truy xuất dữ liệu điều phối từ TemporaryStockExport
                if (request.OrderId == null || request.OrderId == Guid.Empty)
                    throw new Exception("OrderId is required for ImportCoordination.");

                var tempExports = await _tempExportRepo.GetByOrderIdAsync(request.OrderId.Value);

                if (tempExports == null || !tempExports.Any())
                    throw new Exception("Không tìm thấy dữ liệu tạm xuất từ OrderId.");

                foreach (var temp in tempExports)
                {
                    processedBatches.Add(new BatchResponseDto
                    {
                        BatchCode = temp.BatchNumber,
                        ProductId = temp.ProductId,
                        Unit = temp.Batch?.Unit ?? "unit",
                        Quantity = (int)temp.Quantity,
                        UnitCost = temp.UnitPrice,
                        TotalAmount = temp.Quantity * temp.UnitPrice,
                        Status = "PENDING",
                        DateOfManufacture = temp.Batch?.DateOfManufacture ?? DateTime.Now
                    });

                    totalQuantity += (int)temp.Quantity;
                    totalPrice += temp.Quantity * temp.UnitPrice;
                }
                // ✅ Kiểm tra nếu đủ số lượng điều phối thì cập nhật ExportType
                var exportReceipt = await _warehouseExportRepository.GetByOrderIdAsync(request.OrderId.Value);
                if (exportReceipt != null && exportReceipt.ExportType == "PendingTransfer")
                {
                    var exportQuantities = exportReceipt.ExportWarehouseReceiptDetails
                        .GroupBy(x => x.ProductId)
                        .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));

                    var importQuantities = processedBatches
                        .GroupBy(x => x.ProductId)
                        .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));

                    bool isReadyToExport = exportQuantities.All(exportItem =>
                    {
                        var productId = exportItem.Key;
                        var requiredQty = exportItem.Value;

                        return importQuantities.ContainsKey(productId)
                               && importQuantities[productId] >= requiredQty;
                    });

                    if (isReadyToExport)
                    {
                        exportReceipt.ExportType = "PendingExport";
                        await _warehouseExportRepository.UpdateAsync(exportReceipt);
                    }
                }

            }
            else if (request.ImportType == "ImportProduction")
            {
                foreach (var b in request.Batches)
                {
                    processedBatches.Add(new BatchResponseDto
                    {
                        BatchCode = batchCode,
                        ProductId = b.ProductId,
                        Unit = b.Unit,
                        Quantity = b.Quantity,
                        UnitCost = b.UnitCost,
                        TotalAmount = b.Quantity * b.UnitCost,
                        Status = "PENDING",
                        DateOfManufacture = b.DateOfManufacture
                    });

                    totalQuantity += b.Quantity;
                    totalPrice += b.Quantity * b.UnitCost;
                }
            }

            string batchesJson = JsonConvert.SerializeObject(processedBatches, Formatting.Indented);

            var warehouseReceipt = new WarehouseReceipt
            {
                DocumentNumber = request.DocumentNumber,
                DocumentDate = DateTime.Now,
                WarehouseId = request.WarehouseId,
                ImportType = request.ImportType,
                Supplier = request.Supplier,
                DateImport = DateTime.Now,
                TotalQuantity = totalQuantity,
                TotalPrice = totalPrice,
                BatchesJson = batchesJson
            };

            await _receiptRepo.AddAsync(warehouseReceipt);
            await _receiptRepo.SaveChangesAsync();
            return true; // ✅ báo thêm thành công

        }
    }

}
