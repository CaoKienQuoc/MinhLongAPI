using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessObject.Models;
using Repo.IRepository;
using Repo.Repository;
using Services.IService;

namespace Services.Service
{
    public class InventoryService : IInventoryService
    {
        private readonly IWarehouseProductRepository _warehouseProductRepo;
        private readonly ITemporaryWarehouseExportRepository _tempExportRepo;
        private readonly IProductRepository _productRepository;
        private readonly IOrderRepository _orderRepository;

        public InventoryService(
            IWarehouseProductRepository warehouseProductRepo,
            ITemporaryWarehouseExportRepository tempExportRepo,
            IProductRepository productRepository,
            IOrderRepository orderRepository)
        {
            _warehouseProductRepo = warehouseProductRepo;
            _tempExportRepo = tempExportRepo;
            _productRepository = productRepository;
            _orderRepository = orderRepository;
        }


        /*public async Task DeductStockByWarehouseProductAsync(Guid orderId, long productId, long requiredQuantity)
        {
            var remainingQuantity = requiredQuantity;

            var stockList = await _warehouseProductRepo.GetAvailableWarehouseProductsAsync(productId);

            stockList = stockList
                .Where(x => x.ExpirationDate != null)
                .OrderBy(x => x.ExpirationDate)
                .ToList();

            foreach (var stock in stockList)
            {
                if (remainingQuantity <= 0)
                    break;

                var deductQuantity = Math.Min(stock.Quantity, remainingQuantity);

                // Kiểm tra lại tồn kho
                if (stock.Quantity < deductQuantity)
                    throw new InvalidOperationException($"Tồn kho không đủ tại kho {stock.WarehouseId}.");

                stock.Quantity -= (int)deductQuantity;
                await _warehouseProductRepo.UpdateAsync(stock);

                // ✅ Kiểm tra tồn tại bản ghi trong kho tạm
                var existingTemp = await _tempExportRepo.GetByConditionAsync(x =>
                    x.OrderId == orderId &&
                    x.ProductId == productId &&
                    x.WarehouseId == stock.WarehouseId &&
                    !x.IsReverted
                );

                var matchedTemp = existingTemp.FirstOrDefault();
                if (matchedTemp != null)
                {
                    matchedTemp.Quantity += deductQuantity;
                    await _tempExportRepo.UpdateAsync(matchedTemp);
                }
                else
                {
                    await _tempExportRepo.AddAsync(new TemporaryStockExport
                    {
                        ProductId = productId,
                        WarehouseId = stock.WarehouseId,
                        BatchId = stock.BatchId,
                        Quantity = deductQuantity,
                        OrderId = orderId,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                remainingQuantity -= deductQuantity;
            }

            if (remainingQuantity > 0)
                throw new InvalidOperationException($"Không đủ tồn kho cho sản phẩm {productId}. Thiếu {remainingQuantity}");

            await _warehouseProductRepo.SaveChangesAsync();
            await _tempExportRepo.SaveChangesAsync();
        }*/


        public async Task DeductStockByWarehouseProductAsync(Guid orderId, long productId, long requiredQuantity)
        {
            var remainingQuantity = requiredQuantity;

            var stockList = await _warehouseProductRepo.GetAvailableWarehouseProductsAsync(productId);

            stockList = stockList
                .Where(x => x.ExpirationDate != null)
                .OrderBy(x => x.ExpirationDate)
                .ToList();


            foreach (var stock in stockList)
            {

                var unitPrice = stock.Batch?.SellingPrice ?? throw new InvalidOperationException("Không tìm thấy giá lô hàng.");

                if (remainingQuantity <= 0)
                    break;

                var deductQuantity = Math.Min(stock.Quantity, remainingQuantity);

                if (stock.Quantity < deductQuantity)
                    throw new InvalidOperationException($"Tồn kho không đủ tại kho {stock.WarehouseId}.");

                stock.Quantity -= (int)deductQuantity;
                await _warehouseProductRepo.UpdateAsync(stock);

                var existingTemp = await _tempExportRepo.GetByConditionAsync(x =>
                    x.OrderId == orderId &&
                    x.ProductId == productId &&
                    x.WarehouseId == stock.WarehouseId &&
                    x.BatchId == stock.BatchId &&
                    !x.IsReverted
                );

                var matchedTemp = existingTemp.FirstOrDefault();
                if (matchedTemp != null)
                {
                    matchedTemp.Quantity += deductQuantity;
                    await _tempExportRepo.UpdateAsync(matchedTemp);
                }
                else
                {
                    await _tempExportRepo.AddAsync(new TemporaryStockExport
                    {
                        ProductId = productId,
                        WarehouseId = stock.WarehouseId,
                        BatchId = stock.BatchId,
                        BatchNumber = stock.Batch.BatchCode,
                        UnitPrice = unitPrice,
                        ExpiryDate = stock.ExpirationDate,
                        WarehouseProductId = stock.WarehouseProductId,
                        Quantity = deductQuantity,
                        OrderId = orderId,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                remainingQuantity -= deductQuantity;
            }

            if (remainingQuantity > 0)
                throw new InvalidOperationException($"Không đủ tồn kho cho sản phẩm {productId}. Thiếu {remainingQuantity}");

            await _warehouseProductRepo.SaveChangesAsync();
            await _tempExportRepo.SaveChangesAsync();
        }


        public async Task RollbackStockForCancelledOrderAsync(Guid orderId)
        {
            // 1. Lấy danh sách các bản ghi từ bảng tạm ứng với OrderId của đơn bị hủy
            var tempExports = await _tempExportRepo.GetByOrderIdAsync(orderId);
            if (tempExports == null || !tempExports.Any())
            {
                // Không có dữ liệu nào trong bảng tạm => không cần xử lý gì thêm
                return;
            }

            // 2. Kiểm tra nếu có bản ghi nào trong TemporaryStockExport có IsReverted == true
            if (tempExports.Any(te => te.IsReverted))
            {
                // Nếu đơn hàng đã được điều phối (IsReverted == true), thông báo lỗi và dừng xử lý rollback
                throw new InvalidOperationException("Đơn hàng đã được điều phối và không thể huỷ điều phối lại.");
            }

            // 2. Duyệt từng bản ghi tạm để hoàn tác lại số lượng đã trừ ở từng kho
            foreach (var tempExport in tempExports)
            {
                // Lấy bản ghi WarehouseProduct tương ứng (giả sử có 1 bản ghi duy nhất với ProductId và WarehouseId)
                var warehouseProduct = await _warehouseProductRepo.GetByProductWarehouseBatchAsync(
                    tempExport.ProductId, 
                    tempExport.WarehouseId
                    ,tempExport.BatchId);
                if (warehouseProduct != null)
                {
                    // Cộng số lượng đã trừ vào kho
                    warehouseProduct.Quantity += (int)tempExport.Quantity; // Nếu Quantity là int (cần ép kiểu nếu deductQuantity là long)
                    await _warehouseProductRepo.UpdateAsync(warehouseProduct);
                }
            }

            await _warehouseProductRepo.SaveChangesAsync();

            // 3. Xoá các bản ghi trong bảng tạm theo OrderId
            await _tempExportRepo.DeleteByOrderIdAsync(orderId);
            await _tempExportRepo.SaveChangesAsync();

            /*// 4. Cập nhật lại AvailableStock của các sản phẩm liên quan
            // Lấy danh sách ProductId duy nhất từ các bản ghi tạm vừa xử lý
            var productIds = tempExports.Select(te => te.ProductId).Distinct();

            foreach (var productId in productIds)
            {
                // Tính lại tổng tồn kho của sản phẩm từ bảng WarehouseProduct (chỉ tính các bản ghi có Status = 'ACTIVE')
                var totalAvailable = await _warehouseProductRepo.GetTotalAvailableStockByProductIdAsync(productId);

                // Lấy thông tin sản phẩm
                var product = await _productRepository.GetByIdAsync(productId);
                if (product != null)
                {
                    // Cập nhật AvailableStock (ép kiểu nếu cần)
                    product.AvailableStock = (int)totalAvailable;
                    await _productRepository.UpdateAsync(product);
                }
            }
            await _productRepository.SaveChangesAsync();*/
        }

    }


}
