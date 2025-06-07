using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessObject.DTO.Product;
using BusinessObject.DTO.Warehouse;
using BusinessObject.Models;
using Microsoft.EntityFrameworkCore;
using Repo.IRepository;
using Repo.Repository;
using Services.IService;

namespace Services.Service
{
    public class BatchService : IBatchService
    {
        private readonly IBatchRepository _batchRepository;
        private readonly IProductRepository _productRepository;
        private readonly IWarehouseProductRepository _warehouseProductRepo;
        private readonly IWarehouseReceiptRepository _warehouseReceiptRepo;
        private readonly IDamagedStockRepository _damegedStockRepo;

        public BatchService(IBatchRepository batchRepository, 
            IWarehouseProductRepository warehouseProductRepo, 
            IProductRepository productRepository, 
            IWarehouseReceiptRepository warehouseReceiptRepo, 
            IDamagedStockRepository damegedStockRepo)
        {
            _batchRepository = batchRepository;
            _warehouseProductRepo = warehouseProductRepo;
            _productRepository = productRepository;
            _warehouseReceiptRepo = warehouseReceiptRepo;
            _damegedStockRepo = damegedStockRepo;
        }

        public async Task<Batch> GetBatchByIdAsync(long batchId)
        {
            return await _batchRepository.GetByIdAsync(batchId);
        }

        public async Task<IEnumerable<Batch>> GetAllBatchesAsync()
        {
            return await _batchRepository.GetAllAsync();
        }

        /*public async Task<Batch> UpdateBatchAsync(UpdateBatchDto dto, Guid userId, long batchId)
        {

            TimeZoneInfo vietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            DateTime vietnamNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vietnamTimeZone);

            var warehouseProduct = await _warehouseProductRepo.GetWarehouseProductByBatchIdAsync(batchId)
                    ?? throw new KeyNotFoundException($"WarehouseProduct not found");

            // 1. Lấy batch
            var batch = await _batchRepository.GetByIdAsync(batchId)
                        ?? throw new KeyNotFoundException($"Batch {batchId} not found");
            
            // 2. ProductId nếu client gửi
            if (dto.ProductId.HasValue)
            {
                if (!await _productRepository.ExistsAsync(dto.ProductId.Value))
                    throw new ArgumentException($"Product {dto.ProductId.Value} not found");
                batch.ProductId = dto.ProductId.Value;
            }

            // 3. Quantity nếu client gửi
            if (dto.Quantity.HasValue)
            {
                batch.Quantity = dto.Quantity.Value;
                warehouseProduct.Quantity = dto.Quantity.Value;

            }

            // 4. ProfitMarginPercent nếu client gửi
            if (dto.ProfitMarginPercent.HasValue)
            {
                batch.ProfitMarginPercent = dto.ProfitMarginPercent.Value;
                var product = await _productRepository.GetByIdAsync(batch.ProductId);

                // Chỉ cập nhật giá bán khi ProfitMarginPercent > 0 và UnitCost > 0
                if (batch.ProfitMarginPercent > 0 && batch.UnitCost > 0)
                {
                    batch.SellingPrice = batch.UnitCost * (1 + batch.ProfitMarginPercent / 100);
                    if (product != null)
                    {
                        product.Price = batch.SellingPrice;
                        product.UpdatedBy = userId; // Cập nhật người sửa
                        product.UpdatedDate = DateTime.Now; // Cập nhật thời gian sửa
                        await _productRepository.UpdatePriceAsync(product);
                    }
                }
                // Nếu ProfitMarginPercent = 0 thì không cập nhật giá bán
            }


            // 5. DateOfManufacture nếu client gửi
            if (dto.DateOfManufacture.HasValue)
            {
                batch.DateOfManufacture = dto.DateOfManufacture.Value;

                // Lấy DefaultExpiration
                int? daysNullable = await _productRepository.GetDefaultExpirationAsync(batch.ProductId);
                int defaultExpiration = daysNullable ?? 720;

                batch.ExpiryDate = batch.DateOfManufacture
                    .AddDays(defaultExpiration)
                    .AddDays(1);

                if (batch.ExpiryDate > vietnamNow)
                {
                    batch.Status = "ACTIVE";
                }
                else if (batch.ExpiryDate < vietnamNow)
                {
                    batch.Status = "EXPIRED";
                }
            }


            // 6. Lưu
            await _batchRepository.SaveChangesAsync();
            await _warehouseProductRepo.SaveChangesAsync();
            return batch;
        }*/

        public async Task<Batch> UpdateBatchAsync(UpdateBatchDto dto, Guid userId, long batchId)
        {
            TimeZoneInfo vietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            DateTime vietnamNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vietnamTimeZone);

            // 1. Lấy batch
            var batch = await _batchRepository.GetByIdAsync(batchId)
                        ?? throw new KeyNotFoundException($"Batch {batchId} not found");

            // 2. ProductId nếu client gửi
            if (dto.ProductId.HasValue)
            {
                if (!await _productRepository.ExistsAsync(dto.ProductId.Value))
                    throw new ArgumentException($"Product {dto.ProductId.Value} not found");
                batch.ProductId = dto.ProductId.Value;
            }

            // 3. Quantity nếu client gửi
            if (dto.Quantity.HasValue)
            {
                batch.Quantity = dto.Quantity.Value;
            }

            // 4. ProfitMarginPercent nếu client gửi
            if (dto.ProfitMarginPercent.HasValue)
            {
                batch.ProfitMarginPercent = dto.ProfitMarginPercent.Value;
                var product = await _productRepository.GetByIdAsync(batch.ProductId);

                // Chỉ cập nhật giá bán khi ProfitMarginPercent > 0 và UnitCost > 0
                if (batch.ProfitMarginPercent > 0 && batch.UnitCost > 0)
                {
                    batch.SellingPrice = batch.UnitCost * (1 + batch.ProfitMarginPercent / 100);
                    if (product != null)
                    {
                        product.Price = batch.SellingPrice;
                        product.UpdatedBy = userId; // Cập nhật người sửa
                        product.UpdatedDate = DateTime.Now; // Cập nhật thời gian sửa
                        await _productRepository.UpdatePriceAsync(product);
                    }
                }
                // Nếu ProfitMarginPercent = 0 thì không cập nhật giá bán
            }

            // 5. DateOfManufacture nếu client gửi
            if (dto.DateOfManufacture.HasValue)
            {
                batch.DateOfManufacture = dto.DateOfManufacture.Value;

                // Lấy DefaultExpiration
                int? daysNullable = await _productRepository.GetDefaultExpirationAsync(batch.ProductId);
                int defaultExpiration = daysNullable ?? 720;

                batch.ExpiryDate = batch.DateOfManufacture
                    .AddDays(defaultExpiration)
                    .AddDays(1);
            }

            // 6. Xử lý trạng thái batch (ưu tiên hết hạn lên trước)
            if (batch.ExpiryDate < vietnamNow)
            {
                batch.Status = "EXPIRED";
            }
            else if (batch.SellingPrice == 0 && batch.ProfitMarginPercent == 0)
            {
                batch.Status = "CALCULATING_PRICE";
            }
            else if (batch.ExpiryDate > vietnamNow && batch.SellingPrice > 0 && batch.ProfitMarginPercent > 0)
            {
                batch.Status = "ACTIVE";
            }

            // 7. Lưu
            await _batchRepository.SaveChangesAsync();
            return batch;
        }


        public async Task<IEnumerable<Batch>> GetBatchesByProductIdAsync(long productId)
        {
            return await _batchRepository.GetBatchesByProductIdAsync(productId);
        }

        public async Task<(bool Success, string Message, object? Data)> UpdateProfitMarginAsync(long batchId, decimal profitMarginPercent, Guid userId)
        {
            if (profitMarginPercent < 0)
                return (false, "Profit margin percentage must be greater than or equal to 0.", null);

            var batch = await _batchRepository.GetByIdAsync(batchId);
            if (batch == null)
                return (false, "Batch not found.", null);

            if (batch.Status != "CALCULATING_PRICE")
                return (false, "Cannot update profit margin. Batch is not in 'CALCULATING_PRICE' state.", null);

            // ✅ Tính lại giá và cập nhật trạng thái
            batch.ProfitMarginPercent = profitMarginPercent;
            batch.SellingPrice = Math.Round(
                                 batch.UnitCost * (1 + (profitMarginPercent / 100)),
                                     0,
                                     MidpointRounding.AwayFromZero
                                 );
            batch.Status = "ACTIVE";

            // ✅ Cập nhật Batch trước
            bool updated = await _batchRepository.UpdateBatchAndRelatedDataAsync(batch);
            if (!updated)
                return (false, "Failed to update batch and related data.", null);

            // ✅ Đồng bộ vào WarehouseProduct
            var warehouseProduct = await _warehouseProductRepo.GetByProductAndBatchAsync(batch.ProductId, batch.BatchId);
            if (warehouseProduct != null)
            {
                warehouseProduct.Quantity += batch.Quantity; // ✅ Cộng thêm số lượng từ batch
                await _warehouseProductRepo.SaveChangesAsync();
            }
            else
            {
                // ✅ Nếu chưa có, tạo mới
                var newWarehouseProduct = new WarehouseProduct
                {
                    ProductId = batch.ProductId,
                    BatchId = batch.BatchId,
                    Quantity = batch.Quantity,
                    Status = "ACTIVE",
                    ExpirationDate = batch.ExpiryDate,
                    WarehouseId = await _batchRepository.GetWarehouseIdByBatchIdAsync(batch.BatchId) // bạn cần biết kho
                };
                await _warehouseProductRepo.AddAsync(newWarehouseProduct);
                await _warehouseProductRepo.SaveChangesAsync();
            }

            // ✅ BỔ SUNG: Update giá SellingPrice vào Product
            var product = await _productRepository.GetByIdAsync(batch.ProductId);
            if (product != null)
            {
                product.Price = batch.SellingPrice;
                product.UpdatedBy = userId; // Cập nhật người sửa
                product.UpdatedDate = DateTime.Now; // Cập nhật thời gian sửa
                await _productRepository.UpdateAsync(product);
            }
            else
            {
                return (false, "Product not found to update price.", null);
            }

            return (true, "Success", new
            {
                batch.BatchId,
                batch.BatchCode,
                batch.UnitCost,
                ProfitMarginPercent = batch.ProfitMarginPercent,
                SellingPrice = batch.SellingPrice,
                Status = batch.Status
            });
        }


        public async Task<ProductInfoByBatchDto?> GetProductInfoByBatchIdAsync(long batchId)
        {
            var batch = await _batchRepository.GetByIdAsync(batchId);

            if (batch == null) return null;

            return new ProductInfoByBatchDto
            {
                ProductName = batch.Product?.ProductName ??
                              (await _batchRepository.GetProductByIdAsync(batch.ProductId))?.ProductName,
                UnitCost = batch.UnitCost
            };
        }

        public async Task<List<BatchDisplayDto>> GetBatchesByWarehouseIdAsync(long warehouseId)
        {
            var batches = await _batchRepository.GetBatchesByWarehouseIdAsync(warehouseId);

            return batches
                .OrderByDescending(b => b.BatchId) // 🛠️ Sort theo BatchId giảm dần
                .Select(b => new BatchDisplayDto
                {
                    BatchId = b.BatchId,
                    ProductId = b.ProductId,
                    ProductName = b.Product.ProductName,
                    BatchCode = b.BatchCode,
                    UnitCost = b.UnitCost,
                    Quantity = b.ImportTransactionDetail.TotalQuantity,
                    DateOfManufacture = b.DateOfManufacture,
                    ExpiryDate = b.ExpiryDate,
                    TotalAmount = b.TotalAmount,
                    SellingPrice = b.SellingPrice ?? 0,
                    ProfitMarginPercent = b.ProfitMarginPercent,
                    Status = b.Status
                })
                .ToList();
        }

        public async Task<int> UpdateExpiredBatchesAsync(DateTime nowVietnamTime)
        {
            // ✅ Lấy batch đã hết hạn và có trạng thái ACTIVE hoặc CALCULATING_PRICE
            var expiredBatches = await _batchRepository.GetQueryable()
                .Where(b =>
                    (b.Status == "ACTIVE" || b.Status == "CALCULATING_PRICE") &&
                    b.ExpiryDate < nowVietnamTime)
                .ToListAsync();

            foreach (var batch in expiredBatches)
            {
                batch.Status = "EXPIRED";
                //batch.UpdatedAt = DateTime.UtcNow; // ✅ Cập nhật thời gian
            }

            // ✅ Chỉ update nếu có batch cần cập nhật
            if (expiredBatches.Any())
            {
                await _batchRepository.UpdateRangeAsync(expiredBatches);
            }

            return expiredBatches.Count;
        }

        public async Task<int> UpdateExpiredSoonBatchesAsync(DateTime nowVietnamTime)
        {
            // ✅ Lấy batch có trạng thái ACTIVE hoặc CALCULATING_PRICE và sắp hết hạn (còn dưới 6 tháng)
            var expiredSoonBatches = await _batchRepository.GetQueryable()
                .Where(b =>
                    (b.Status == "ACTIVE" || b.Status == "CALCULATING_PRICE") &&
                    b.ExpiryDate <= nowVietnamTime.AddMonths(6) &&
                    b.ExpiryDate > nowVietnamTime && // Chỉ lấy batch chưa hết hạn
                    b.Status != "EXPIREDSOON" && // Tránh cập nhật lại batch đã set "EXPIRED_SOON"
                    b.Status != "EXPIRED" // Tránh batch đã hết hạn
                )
                .ToListAsync();

            foreach (var batch in expiredSoonBatches)
            {
                batch.Status = "EXPIREDSOON";
                //batch.UpdatedAt = DateTime.UtcNow; // ✅ Cập nhật thời gian nếu cần
            }

            // ✅ Chỉ update nếu có batch cần cập nhật
            if (expiredSoonBatches.Any())
            {
                await _batchRepository.UpdateRangeAsync(expiredSoonBatches);
            }

            return expiredSoonBatches.Count;
        }



        public async Task<bool> CancelExpiredBatchAsync(long batchId, string? reason = null)
        {
            var batch = await _batchRepository.GetExpiredBatchByIdAsync(batchId);
            if (batch == null)
                return false;

            var importDetail = await _warehouseReceiptRepo.GetImportTransactionDetailByIdAsync(batch.ImportTransactionDetailId);
            if (importDetail == null)
                return false;

            var importTransaction = await _warehouseReceiptRepo.GetImportTransactionByIdAsync(importDetail.ImportTransactionId);
            if (importTransaction == null)
                return false;

            // ✅ Sử dụng lý do được truyền vào, nếu không có thì dùng mặc định "Expired"
            var cancelReceipt = new DamagedStock
            {
                BatchId = batch.BatchId,
                ProductId = batch.ProductId,
                Quantity = batch.Quantity,
                WarehouseId = importTransaction.WarehouseId,
                Reason = reason ?? "Expired",
                CreatedAt = DateTime.Now,
                Status = "ExportCancel"
            };

            // ✅ Lưu vào bảng DamagedStock
            await _damegedStockRepo.AddAsync(cancelReceipt);

            // ✅ Cập nhật trạng thái Batch
            batch.Status = "CANCELED";
            await _batchRepository.UpdateAsync(batch);
            await _damegedStockRepo.SaveChangesAsync();

            return true;
        }

        public async Task<bool> UpdateSoldOutStatusAsync(long batchId)
        {
            return await _batchRepository.UpdateSoldOutStatusAsync(batchId);
        }

        public async Task<bool> UpdateSoldOutStatusForAllBatchesAsync(IEnumerable<long> batchIds)
        {
            bool allUpdated = true;
            foreach (var batchId in batchIds)
            {
                var updated = await _batchRepository.UpdateSoldOutStatusAsync(batchId);
                if (!updated) allUpdated = false;
            }
            return allUpdated;
        }


        public async Task<List<BatchDisplayDto>> GetExpiredSoonBatchesByWarehouseAsync(long warehouseId)
        {
            var batches = await _batchRepository.GetExpiredSoonBatchesByWarehouseAsync(warehouseId);

            return batches
               .Select(b => new BatchDisplayDto
               {
                   BatchId = b.BatchId,
                   ProductId = b.ProductId,
                   ProductName = b.Product.ProductName,
                   BatchCode = b.BatchCode,
                   UnitCost = b.UnitCost,
                   Quantity = b.Quantity,
                   DateOfManufacture = b.DateOfManufacture,
                   ExpiryDate = b.ExpiryDate,
                   TotalAmount = b.TotalAmount,
                   SellingPrice = b.SellingPrice ?? 0,
                   ProfitMarginPercent = b.ProfitMarginPercent,
                   Status = b.Status
               })
               .ToList();
        }
    }
}
