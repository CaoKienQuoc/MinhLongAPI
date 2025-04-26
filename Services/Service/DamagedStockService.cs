using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessObject.DTO.ReturnOrder;
using BusinessObject.Models;
using Repo.IRepository;
using Services.IService;

namespace Services.Service
{
    public class DamagedStockService : IDamagedStockService
    {
        private readonly IDamagedStockRepository _damagedRepo;
        private readonly IReturnWarehouseReceiptRepository _returnWarehouseReceiptRepo;
        private readonly IReturnRequestRepository _returnRepo;
        private readonly IWarehouseRepository _warehouseRepo;

        public DamagedStockService(
            IDamagedStockRepository repo,
            IReturnWarehouseReceiptRepository receiptRepo,
            IReturnRequestRepository reqRepo,
            IWarehouseRepository whRepo)
        {
            _damagedRepo = repo;
            _returnWarehouseReceiptRepo = receiptRepo;
            _returnRepo = reqRepo;
            _warehouseRepo = whRepo;
        }

        public Task<IEnumerable<DamagedStockDto>> GetByWarehouseIdAsync(long warehouseId)
        => _damagedRepo.GetByWarehouseIdAsync(warehouseId);

        public async Task ImportToDamagedStockAsync(long warehouseReceiptId, Guid warehouseUserId)
        {
            // 1) Lấy phiếu trả hàng cùng chi tiết
            var receipt = await _returnWarehouseReceiptRepo
                .GetByIdWithDetailsAsync(warehouseReceiptId);

            if (receipt is null)
                throw new Exception("Không tìm thấy phiếu trả hàng.");
            var returnReceipt = await _returnRepo.GetByIdAsync(receipt.ReturnRequestId);
            if (returnReceipt.Status != "Approved")
                throw new Exception("Phiếu chưa được duyệt.");

            // 2) Lấy WarehouseId từ user
            var warehouseId = await _warehouseRepo.GetWarehouseIdByUserAsync(warehouseUserId);
            if (warehouseId == 0)
                throw new Exception("Không tìm thấy kho tương ứng với người dùng.");

            // 3) Chuyển chi tiết thành DamagedStock
            var damagedStocks = receipt.Details.Select(d => new DamagedStock
            {
                ProductId = d.ProductId,
                WarehouseId = warehouseId,
                Quantity = d.Quantity,
                BatchId = d.BatchId,
                CreatedAt = DateTime.UtcNow,
                // nếu bạn có trường Reason hay BatchCode, gán thêm ở đây
            }).ToList();

            // 4) Lưu vào table DamagedStock
            await _damagedRepo.AddRangeAsync(damagedStocks);

            // 5) Cập nhật trạng thái phiếu
            await _returnWarehouseReceiptRepo.UpdateStatusAsync(warehouseReceiptId, "Imported");

            // 6) Cập nhật trạng thái trên ReturnRequest thành "Completed"
            await _returnRepo.UpdateStatusAsync(receipt.ReturnRequestId, "Completed");
        }
    }
}
