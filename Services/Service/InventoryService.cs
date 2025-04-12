using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessObject.Models;
using Repo.IRepository;
using Services.IService;

namespace Services.Service
{
    public class InventoryService : IInventoryService
    {
        private readonly IWarehouseProductRepository _warehouseProductRepo;
        private readonly ITemporaryWarehouseExportRepository _tempExportRepo;

        public InventoryService(
            IWarehouseProductRepository warehouseProductRepo,
            ITemporaryWarehouseExportRepository tempExportRepo)
        {
            _warehouseProductRepo = warehouseProductRepo;
            _tempExportRepo = tempExportRepo;
        }

        public async Task DeductStockByWarehouseProductAsync(Guid orderId, long productId, long requiredQuantity)
        {
            var remainingQuantity = requiredQuantity;

            var stockList = await _warehouseProductRepo.GetAvailableWarehouseProductsAsync(productId);

            foreach (var stock in stockList)
            {
                if (remainingQuantity <= 0)
                    break;

                var deductQuantity = Math.Min(stock.Quantity, remainingQuantity);
                stock.Quantity -= (int)deductQuantity;


                await _warehouseProductRepo.UpdateAsync(stock);

                var tempExport = new TemporaryStockExport
                {
                    ProductId = productId,
                    WarehouseId = stock.WarehouseId,
                    Quantity = deductQuantity,
                    OrderId = orderId,
                    CreatedAt = DateTime.UtcNow
                };

                await _tempExportRepo.AddAsync(tempExport);

                remainingQuantity -= deductQuantity;
            }

            if (remainingQuantity > 0)
            {
                throw new InvalidOperationException($"Không đủ tồn kho cho sản phẩm {productId}. Còn thiếu {remainingQuantity}.");
            }

            await _warehouseProductRepo.SaveChangesAsync();
            await _tempExportRepo.SaveChangesAsync();
        }
    }


}
