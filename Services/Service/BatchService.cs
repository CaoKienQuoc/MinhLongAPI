using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessObject.DTO.Product;
using BusinessObject.Models;
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

        public BatchService(IBatchRepository batchRepository, IWarehouseProductRepository warehouseProductRepo, IProductRepository productRepository)
        {
            _batchRepository = batchRepository;
            _warehouseProductRepo = warehouseProductRepo;
            _productRepository = productRepository;
        }

        public async Task<Batch> GetBatchByIdAsync(long batchId)
        {
            return await _batchRepository.GetByIdAsync(batchId);
        }

        public async Task<IEnumerable<Batch>> GetAllBatchesAsync()
        {
            return await _batchRepository.GetAllAsync();
        }

        public async Task<bool> UpdateBatchAsync(Batch batch)
        {
            return await _batchRepository.UpdateAsync(batch);
        }

        public async Task<IEnumerable<Batch>> GetBatchesByProductIdAsync(long productId)
        {
            return await _batchRepository.GetBatchesByProductIdAsync(productId);
        }

        public async Task<(bool Success, string Message, object? Data)> UpdateProfitMarginAsync(long batchId, decimal profitMarginPercent)
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
            batch.SellingPrice = batch.UnitCost * (1 + (profitMarginPercent / 100));
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
            return batches.Select(b => new BatchDisplayDto
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
            }).ToList();
        }
    }
}
