using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessObject.Models;
using Newtonsoft.Json;
using Repo.IRepository;
using Services.IService;

namespace Services.Service
{
    public class WarehouseReceiptService : IWarehouseReceiptService
    {
        private readonly IWarehouseReceiptRepository _receiptRepo;
        private readonly ITemporaryWarehouseExportRepository _tempExportRepo;
        private readonly IBatchRepository _batchRepo;

        public WarehouseReceiptService(
            IWarehouseReceiptRepository receiptRepo,
            ITemporaryWarehouseExportRepository tempExportRepo,
            IBatchRepository batchRepo)
        {
            _receiptRepo = receiptRepo;
            _tempExportRepo = tempExportRepo;
            _batchRepo = batchRepo;
        }

        public async Task<WarehouseReceipt> CreateWarehouseReceiptFromCoordinationAsync(long warehouseId)
        {
            var tempExports = await _tempExportRepo.GetByWarehouseIdAsync(warehouseId);
            if (tempExports == null || !tempExports.Any())
                throw new InvalidOperationException("Không có dữ liệu điều phối vào kho này.");

            var batches = new List<object>();
            decimal totalQuantity = 0;
            decimal totalAmount = 0;

            foreach (var item in tempExports)
            {
                var batch = await _batchRepo.GetByIdAsync(item.BatchId);
                if (batch == null)
                    throw new InvalidOperationException($"Không tìm thấy batch với ID: {item.BatchId}");

                batches.Add(new
                {
                    item.ProductId,
                    item.BatchId,
                    batch.BatchCode,
                    Quantity = (decimal)item.Quantity,
                    item.UnitPrice,
                    batch.SellingPrice,
                    batch.ExpiryDate
                });

                totalQuantity += (decimal)item.Quantity;
                totalAmount += item.UnitPrice * (decimal)item.Quantity;
            }

            var receipt = new WarehouseReceipt
            {
                DocumentNumber = $"PNK-Coord-{DateTime.UtcNow.Ticks}",
                DocumentDate = DateTime.UtcNow,
                WarehouseId = warehouseId,
                ImportType = "ImportCoordination",
                Supplier = "Điều phối nội bộ",
                DateImport = DateTime.UtcNow,
                TotalQuantity = (int)totalQuantity,
                TotalPrice = totalAmount,
                BatchesJson = JsonConvert.SerializeObject(batches),
                Note = "Nhập hàng từ điều phối",
                IsApproved = true
            };

            await _receiptRepo.AddAsync(receipt);
            await _receiptRepo.SaveChangesAsync();

            return receipt;
        }
    }

}
