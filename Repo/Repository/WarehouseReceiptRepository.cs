using BusinessObject.DTO.Product;
using BusinessObject.Models;
using DataAccessLayer;
using Microsoft.AspNetCore.Http;
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
    public class WarehouseReceiptRepository : IWarehouseReceiptRepository
    {
        private readonly MinhLongDbContext _context;

        public WarehouseReceiptRepository(MinhLongDbContext context)
        {
            _context = context;
        }


        public async Task<bool> AddAsync(WarehouseReceipt warehouseReceipt)
        {
            _context.WarehouseReceipts.Add(warehouseReceipt);
            await _context.SaveChangesAsync();
            return true;
        }


        public async Task<WarehouseReceipt> GetByIdAsync(long id)
        {
            return await _context.WarehouseReceipts.FindAsync(id);

        }

        public async Task<bool> ApproveAsync(long id, Guid currentUserId)
        {
            var warehouseReceipt = await _context.WarehouseReceipts.FindAsync(id);
            if (warehouseReceipt.IsApproved == true)
            {
                throw new Exception("The order was previously activated!");
            }

            var warehouseUserId = await _context.Warehouses
                .Where(w => w.WarehouseId == warehouseReceipt.WarehouseId)
                .Select(w => w.UserId)
                .FirstOrDefaultAsync();

            if (warehouseUserId != currentUserId)
            {
                throw new BadHttpRequestException("Kho này không phải kho của bạn! Bạn không có quyền duyệt phiếu nhập này.");
            }

            var receipt = await _context.WarehouseReceipts
                .Where(w => w.WarehouseReceiptId == id)
                .Select(w => new
                {
                    w.DocumentNumber,
                    w.DocumentDate,
                    w.WarehouseId,
                    w.ImportType,
                    w.Supplier,
                    w.DateImport,
                    w.Note,
                    w.TotalQuantity,
                    w.TotalPrice,
                    w.BatchesJson
                })
                .FirstOrDefaultAsync();

            if (receipt == null) return false;

            var importTransaction = new ImportTransaction
            {
                DocumentNumber = receipt.DocumentNumber,
                DocumentDate = receipt.DocumentDate,
                WarehouseId = receipt.WarehouseId,
                TypeImport = receipt.ImportType,
                Supplier = receipt.Supplier,
                DateImport = receipt.DateImport,
                Note = receipt.Note,
            };

            _context.ImportTransactions.Add(importTransaction);
            await _context.SaveChangesAsync();

            var importDetail = await _context.ImportTransactionDetails
                .FirstOrDefaultAsync(d => d.ImportTransactionId == importTransaction.ImportTransactionId);

            if (importDetail == null)
            {
                importDetail = new ImportTransactionDetail
                {
                    ImportTransactionId = importTransaction.ImportTransactionId,
                    TotalQuantity = receipt.TotalQuantity,
                    TotalPrice = receipt.TotalPrice,
                };

                _context.ImportTransactionDetails.Add(importDetail);
                await _context.SaveChangesAsync();
            }

            long importTransactionDetailId = importDetail.ImportTransactionDetailId;

            var batches = JsonConvert.DeserializeObject<List<BatchRequest>>(receipt.BatchesJson);
            var updatedBatchDtos = new List<BatchResponseDto>();

            foreach (var batch in batches)
            {
                var product = await _context.Products.FindAsync(batch.ProductId);
                if (product == null)
                {
                    throw new Exception($"Product with ID {batch.ProductId} not found.");
                }

                var newBatch = new Batch
                {
                    ImportTransactionDetailId = importTransactionDetailId,
                    BatchCode = batch.BatchCode,
                    ProductId = batch.ProductId,
                    Unit = batch.Unit,
                    Quantity = batch.Quantity,
                    UnitCost = batch.UnitCost,
                    TotalAmount = batch.UnitCost * batch.Quantity,
                    DateOfManufacture = batch.DateOfManufacture,
                    ExpiryDate = batch.DateOfManufacture.AddDays(product.DefaultExpiration ?? 0),
                    Status = "CALCULATING_PRICE"
                };

                _context.Batches.Add(newBatch);
                await _context.SaveChangesAsync();

                var inventory = new WarehouseProduct
                {
                    ProductId = batch.ProductId,
                    WarehouseId = receipt.WarehouseId,
                    BatchId = newBatch.BatchId,
                    ExpirationDate = newBatch.ExpiryDate,
                    Quantity = batch.Quantity,
                    Status = newBatch.Status
                };

                _context.WarehouseProduct.Add(inventory);
                product.AvailableStock += batch.Quantity;

                // ✅ Thêm vào danh sách cập nhật BatchesJson
                updatedBatchDtos.Add(new BatchResponseDto
                {
                    BatchCode = newBatch.BatchCode,
                    ProductId = newBatch.ProductId,
                    Unit = newBatch.Unit,
                    Quantity = newBatch.Quantity,
                    UnitCost = newBatch.UnitCost,
                    TotalAmount = newBatch.TotalAmount,
                    Status = newBatch.Status,
                    DateOfManufacture = newBatch.DateOfManufacture
                });

                await _context.SaveChangesAsync();
            }

            // ✅ Cập nhật lại BatchesJson với trạng thái thực tế
            warehouseReceipt.BatchesJson = JsonConvert.SerializeObject(updatedBatchDtos, Formatting.Indented);
            warehouseReceipt.IsApproved = true;

            await _context.SaveChangesAsync();

            return true;
        }


        public async Task<List<WarehouseReceipt>> GetAllAsync()
        {
            return await _context.WarehouseReceipts.ToListAsync();
        }

        public async Task<List<WarehouseReceipt>> GetWarehouseReceiptDTOIdAsync(long Id)
        {
            return await _context.WarehouseReceipts
                .Where(rp => rp.WarehouseId == Id) // ✅ Lọc theo AgencyId
                .ToListAsync();
        }

        public async Task<WarehouseTransferRequest?> GetTransferRequestWithExportDetailsAsync(long transferRequestId)
        {
            return await _context.WarehouseTransferRequests
                .Include(r => r.TransferProducts)
                .FirstOrDefaultAsync(r => r.Id == transferRequestId);
        }

        public async Task<Guid?> GetUserIdOfWarehouseAsync(long warehouseId)
        {
            return await _context.Warehouses
                .Where(w => w.WarehouseId == warehouseId)
                .Select(w => w.UserId)
                .FirstOrDefaultAsync();
        }

        public async Task<WarehouseReceipt> CreateReceiptAsync(WarehouseReceipt receipt)
        {
            _context.WarehouseReceipts.Add(receipt);
            await _context.SaveChangesAsync();
            return receipt;
        }

        public async Task<ExportWarehouseReceipt?> GetApprovedExportReceiptByTransferIdAsync(long transferRequestId)
        {
            return await _context.ExportWarehouseReceipts
                .Include(e => e.ExportWarehouseReceiptDetails)
                .FirstOrDefaultAsync(r =>
                    r.WarehouseTransferRequestId == transferRequestId &&
                    r.Status == "Approved");
        }

    }

}
