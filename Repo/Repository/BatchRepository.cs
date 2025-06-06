using BusinessObject.DTO.Product;
using BusinessObject.Models;
using DataAccessLayer;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Repo.IRepository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repo.Repository
{
    public class BatchRepository : IBatchRepository
    {
        private readonly MinhLongDbContext _context;

        public BatchRepository(MinhLongDbContext context)
        {
            _context = context;
        }

        public async Task<List<Batch>> GetListBatchesByIdsAsync(List<long> batchIds)
        {
            if (batchIds == null || !batchIds.Any())
                return new List<Batch>();

            return await _context.Batches
                .Where(b => batchIds.Contains(b.BatchId))
                .ToListAsync();
        }


        public async Task<Dictionary<long, decimal>> GetHighestSellingPricesByProductIdsAsync(List<long> productIds)
        {
            return await _context.Batches
                .Where(b => productIds.Contains(b.ProductId) && b.Status == "ACTIVE")
                .GroupBy(b => b.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    MaxSellingPrice = g.Max(x => x.SellingPrice)
                })
                .ToDictionaryAsync(
                    x => x.ProductId,
                    x => x.MaxSellingPrice ?? 0m // xử lý nullable
                );
        }


        public async Task<Batch?> GetLatestBatchByProductIdAsync(long productId)
        {
            /*return await _context.Batches
                .Where(b => b.ProductId == productId)
                .OrderByDescending(b => b.ExpiryDate)
                .AsQueryable() // ✅ Chuyển về IQueryable trước khi gọi FirstOrDefaultAsync
                .FirstOrDefaultAsync();*/

            // ✅ Tính thời gian 6 tháng sau từ hiện tại
            // ✅ Tính thời gian 6 tháng sau theo giờ Việt Nam
            TimeZoneInfo vnTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            DateTime vnNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vnTimeZone);
            DateTime sixMonthsLater = vnNow.AddMonths(6);

            // ✅ Lọc batch theo ProductId, Status và ExpiryDate
            return await _context.Batches
                .Where(b =>
                    b.ProductId == productId &&
                    b.Status == "ACTIVE" &&
                    b.ExpiryDate > sixMonthsLater
                )
                .OrderBy(b => b.ExpiryDate) // ✅ Sắp xếp từ ngày gần hết hạn nhất
                .FirstOrDefaultAsync();
        }

        public async Task<Batch> GetByIdAsync(long batchId)
        {
            return await _context.Batches.FirstOrDefaultAsync(b => b.BatchId == batchId);
        }

        public async Task<IEnumerable<Batch>> GetAllAsync()
        {
            return await _context.Batches.ToListAsync();
        }

        public async Task<bool> UpdateAsync(Batch batch)
        {
            _context.Batches.Update(batch);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<Batch>> GetBatchesByIdsAsync(List<long> batchIds)
        {
            return await _context.Batches
                .Where(b => batchIds.Contains(b.BatchId))
                .ToListAsync();
        }

        public async Task<IEnumerable<Batch>> GetBatchesByProductIdAsync(long productId)
        {
            return await _context.Batches
                .Where(b => b.ProductId == productId)
                .ToListAsync();
        }

        public async Task<int> CountBatchesByDateAsync(DateTime date)
        {
            string datePart = date.ToString("yyyyMMdd");

            return await _context.Batches
                .CountAsync(b => b.BatchCode.StartsWith($"BA{datePart}"));
        }

        public async Task<bool> UpdateBatchAndRelatedDataAsync(Batch batch)
        {
            // ✅ Cập nhật chính bản ghi Batch
            _context.Batches.Update(batch);

            // ✅ Cập nhật trạng thái các dòng WarehouseProduct liên quan
            var warehouseProducts = await _context.WarehouseProduct
                .Where(wp => wp.BatchId == batch.BatchId)
                .ToListAsync();

            foreach (var wp in warehouseProducts)
            {
                wp.Status = batch.Status;
            }

            // ✅ Cập nhật trạng thái trong BatchesJson của WarehouseReceipt
            var warehouseReceipt = await _context.WarehouseReceipts
                .Where(wr => wr.BatchesJson.Contains(batch.BatchCode))
                .FirstOrDefaultAsync();

            if (warehouseReceipt != null && !string.IsNullOrEmpty(warehouseReceipt.BatchesJson))
            {
                var batchList = JsonConvert.DeserializeObject<List<BatchResponseDto>>(warehouseReceipt.BatchesJson);

                // ✅ Cập nhật tất cả các batch có cùng BatchCode
                foreach (var b in batchList.Where(b => b.BatchCode == batch.BatchCode))
                {
                    b.Status = batch.Status;
                }

                warehouseReceipt.BatchesJson = JsonConvert.SerializeObject(batchList, Formatting.Indented);
            }

            var product = await _context.Products.FindAsync(batch.ProductId);
            if (product != null)
            {
                product.Price = batch.SellingPrice;
                _context.Products.Update(product);
            }

            // ✅ Lưu tất cả thay đổi
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<Product?> GetProductByIdAsync(long productId)
        {
            return await _context.Products.FindAsync(productId);
        }

        public async Task AddAsync(Batch entity)
        {
            await _context.Batches.AddAsync(entity);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task<long> GetWarehouseIdByBatchIdAsync(long batchId)
        {
            var batch = await _context.Batches
                .Include(b => b.ImportTransactionDetail)
                    .ThenInclude(d => d.ImportTransaction)
                .FirstOrDefaultAsync(b => b.BatchId == batchId);

            if (batch?.ImportTransactionDetail?.ImportTransaction == null)
                throw new Exception("Không thể truy xuất WarehouseId từ Batch.");

            return batch.ImportTransactionDetail.ImportTransaction.WarehouseId;
        }

        //public async Task<List<Batch>> GetBatchesByWarehouseIdAsync(long warehouseId)
        //{
        //    return await _context.Batches
        //        .Include(b => b.ImportTransactionDetail)
        //        .Include(b => b.Product)
        //        .Where(b => b.ImportTransactionDetail.ImportTransaction.WarehouseId == warehouseId)
        //        .ToListAsync();
        //}


        public async Task<List<Batch>> GetBatchesByWarehouseIdAsync(long warehouseId)
        {
            return await _context.Batches
                .Include(b => b.ImportTransactionDetail)
                    .ThenInclude(d => d.ImportTransaction)
                .Include(b => b.Product)
                .Where(b => b.ImportTransactionDetail.ImportTransaction.WarehouseId == warehouseId)
                .ToListAsync();
        }


        public IQueryable<Batch> GetQueryable()
        {
            return _context.Batches.AsQueryable();
        }

        public async Task UpdateRangeAsync(IEnumerable<Batch> batches)
        {
            _context.Batches.UpdateRange(batches);
            await _context.SaveChangesAsync();
        }

        public async Task<Batch> GetExpiredBatchByIdAsync(long batchId)
        {
            return await _context.Batches
                .FirstOrDefaultAsync(b => b.BatchId == batchId && b.Status == "EXPIRED");
        }

        public async Task DeleteAsync(Batch batch)
        {
            _context.Batches.Remove(batch);
            await Task.CompletedTask; // hoặc bạn có thể bỏ nếu không async
        }

        public async Task UpdateBatchAsync(Batch batch)
        {
            _context.Batches.Update(batch);
            //await _context.SaveChangesAsync();
        }

        public async Task<bool> UpdateSoldOutStatusAsync(long batchId)
        {
            var batch = await _context.Batches.FindAsync(batchId);
            if (batch == null) return false;

            // Kiểm tra số lượng tồn trong WarehouseProduct
            var warehouseProduct = await _context.WarehouseProduct
                .Where(wp => wp.BatchId == batchId)
                .ToListAsync();

            if (warehouseProduct.All(wp => wp.Quantity == 0))
            {
                batch.Status = "SOLD_OUT";
                batch.SoldOut = true;
                _context.Batches.Update(batch);
                await _context.SaveChangesAsync();
                return true;
            }

            return false;
        }

        public async Task<List<Batch>> GetExpiredSoonBatchesByWarehouseAsync(long warehouseId)
        {
            return await _context.Batches
                .Include(b => b.ImportTransactionDetail)
                .Include(b => b.Product)
                .Where(b =>
                    b.Status == "EXPIRED_SOON" &&
                    _context.WarehouseProduct.Any(wp =>
                        wp.BatchId == b.BatchId &&
                        wp.WarehouseId == warehouseId
                    )
                )
                .OrderBy(b => b.ExpiryDate)
                .ToListAsync();
        }

        public async Task<List<Batch>> GetExpiredSoonBatchesAsync()
        {
            return await _context.Batches
                .Where(b => b.Status == "EXPIRED_SOON")
                .Include(b => b.Product)
                    .ThenInclude(p => p.Images)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<Batch>> GetExpiredSoonBatchesByCategoryAsync(long categoryId)
        {
            return await _context.Batches
                .Where(b => b.Status == "EXPIRED_SOON" && b.Product.CategoryId == categoryId)
                .Include(b => b.Product)
                    .ThenInclude(p => p.Images)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<Batch>> GetExpiredSoonBatchesByProductIdAsync(long productId)
        {
            return await _context.Batches
                .Where(b => b.ProductId == productId && b.Status == "EXPIRED_SOON")
                .Include(b => b.Product)
                    .ThenInclude(p => p.Images)
                .AsNoTracking()
                .ToListAsync();
        }



    }

}
