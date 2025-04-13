using BusinessObject.DTO.RequestExport;
using BusinessObject.DTO.Warehouse;
using BusinessObject.Models;
using DataAccessLayer;
using Microsoft.EntityFrameworkCore;
using Repo.IRepository;
using Services.IService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Service
{
    public class ExportWarehouseReceiptService : IExportWarehouseReceiptService
    {
        private readonly IExportWarehouseReceiptRepository _repository;
        private readonly IWarehouseTransferRepository _transferRequestRepo;
        private readonly MinhLongDbContext _context;
        private readonly PdfService _pdfService;

        public ExportWarehouseReceiptService(IExportWarehouseReceiptRepository repository,  MinhLongDbContext context, IWarehouseTransferRepository transferRequestRepo, PdfService pdfService)
        {
            _repository = repository;
            _context = context;
            _transferRequestRepo = transferRequestRepo;
            _pdfService = pdfService;
        }

        public async Task<ExportWarehouseReceipt> CreateReceiptAsync(ExportWarehouseReceiptDTO dto)
        {
            var warehouseProducts = await _repository.GetWarehouseProductsByIdsAsync(
                dto.Details.Select(d => d.WarehouseProductId).ToList()
            );

            foreach (var detail in dto.Details)
            {
                var warehouseProduct = warehouseProducts.FirstOrDefault(p => p.WarehouseProductId == detail.WarehouseProductId);
                if (warehouseProduct == null)
                {
                    throw new Exception($"WarehouseProductId {detail.WarehouseProductId} not found in database.");
                }

                if (warehouseProduct.Batch == null || string.IsNullOrEmpty(warehouseProduct.Batch.BatchCode))
                {
                    throw new Exception($"BatchNumber is missing for WarehouseProductId {detail.WarehouseProductId}.");
                }

                if (warehouseProduct.Quantity < detail.Quantity)
                {
                    throw new Exception($"Insufficient quantity for WarehouseProductId {detail.WarehouseProductId}. Available: {warehouseProduct.Quantity}, Requested: {detail.Quantity}");
                }
            }

            var requestExport = await _context.RequestExports
        .Include(r => r.Order)
            .ThenInclude(o => o.RequestProduct)
            .ThenInclude(x => x.AgencyAccount)
        .FirstOrDefaultAsync(r => r.RequestExportId == dto.RequestExportId);

            if (requestExport == null)
            {
                throw new Exception($"RequestExportId {dto.RequestExportId} not found in database.");
            }

            var receiptDetails = dto.Details.Select(d =>
            {
                var warehouseProduct = warehouseProducts.First(p => p.WarehouseProductId == d.WarehouseProductId);
                return new ExportWarehouseReceiptDetail
                {
                    WarehouseProductId = d.WarehouseProductId,
                    ProductId = warehouseProduct.ProductId,
                    ProductName = warehouseProduct.Product.ProductName,
                    BatchNumber = warehouseProduct.Batch.BatchCode,
                    Quantity = d.Quantity,
                    UnitPrice = warehouseProduct.Batch.SellingPrice ?? 0,
                    TotalProductAmount = d.Quantity * (warehouseProduct.Batch.SellingPrice ?? 0),
                    ExpiryDate = warehouseProduct.ExpirationDate
                };
            }).ToList();

            var receipt = new ExportWarehouseReceipt
            {
                DocumentNumber = "EXP-" + DateTime.Now.ToString("yyyyMMdd-HHmmss"),
                DocumentDate = DateTime.Now,
                ExportDate = dto.ExportDate,
                ExportType = dto.ExportType,
                WarehouseId = dto.WarehouseId,
                Status = "Pending",
                TotalQuantity = receiptDetails.Sum(d => d.Quantity),
                TotalAmount = receiptDetails.Sum(d => d.TotalProductAmount),
                ExportWarehouseReceiptDetails = receiptDetails,
                RequestExportId = dto.RequestExportId,
                OrderCode = requestExport.Order.OrderCode, // 🔥 Lấy từ RequestExport
                AgencyName = requestExport.Order.RequestProduct.AgencyAccount.AgencyName // 🔥 Lấy từ SalesAgent
            };

            return await _repository.CreateReceiptAsync(receipt);
        }

        public async Task ApproveReceiptAsync(long id, Guid approvedBy)
        {
            var receipt = await _repository.GetReceiptByIdAsync(id);
            if (receipt == null) throw new Exception("Receipt not found");

            receipt.Status = "Approved";

            var warehouseProducts = await _repository.GetWarehouseProductsByIdsAsync(
                receipt.ExportWarehouseReceiptDetails.Select(d => d.WarehouseProductId).ToList()
            );

            foreach (var detail in receipt.ExportWarehouseReceiptDetails)
            {
                var warehouseProduct = warehouseProducts.First(wp => wp.WarehouseProductId == detail.WarehouseProductId);

                if (warehouseProduct.Quantity < detail.Quantity)
                    throw new Exception($"Insufficient quantity in warehouse for product {warehouseProduct.ProductId}");

                warehouseProduct.Quantity -= detail.Quantity;

                var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == warehouseProduct.ProductId);
                if (product != null)
                {
                    if (product.AvailableStock < detail.Quantity)
                        throw new Exception($"Product stock is insufficient for product {product.ProductId}");

                    product.AvailableStock -= detail.Quantity;
                    _context.Products.Update(product);
                }
            }

            // Gộp số lượng xuất theo ProductId
            var groupedExportDetails = receipt.ExportWarehouseReceiptDetails
                .GroupBy(d => d.ProductId)
                .ToDictionary(g => g.Key, g => g.Sum(d => d.Quantity));

            // Nếu là xuất bán → cập nhật WarehouseRequestExport
            if (receipt.ExportType == "Xuất Bán")
            {
                var requestExports = await _context.WarehouseRequestExports
                    .Where(x => x.RequestExportId == receipt.RequestExportId && x.WarehouseId == receipt.WarehouseId)
                    .ToListAsync();

                foreach (var wr in requestExports)
                {
                    if (groupedExportDetails.TryGetValue(wr.ProductId, out int approvedQty))
                    {
                        wr.RemainingQuantity = wr.QuantityRequested - approvedQty;
                        wr.Status = "APPROVED";
                        wr.RequestedBy = approvedBy;
                    }
                }

                _context.WarehouseRequestExports.UpdateRange(requestExports);

                // Cập nhật trạng thái RequestExport
                var totalRemaining = requestExports.Sum(x => x.RemainingQuantity);
                var requestExport = await _context.RequestExports
                    .FirstOrDefaultAsync(x => x.RequestExportId == receipt.RequestExportId);

                if (requestExport != null)
                {
                    requestExport.Status = totalRemaining == 0 ? "Approved" : "Partially_Exported";
                    _context.RequestExports.Update(requestExport);
                }
            }

            // Ghi lại ExportTransaction
            var exportTransaction = new ExportTransaction
            {
                DocumentNumber = receipt.DocumentNumber,
                DocumentDate = receipt.DocumentDate,
                ExportDate = receipt.ExportDate,
                ExportType = receipt.ExportType,
                WarehouseId = receipt.WarehouseId,
                Note = "Approved Export",
                RequestExportId = receipt.RequestExportId,
                OrderCode = receipt.OrderCode, // vẫn có thể dùng nếu bạn muốn lưu cho trace
                AgencyName = receipt.ExportType == "Xuất Bán" ? receipt.RequestExport.Order?.RequestProduct?.AgencyAccount?.AgencyName : null,
                ExportTransactionDetail = receipt.ExportWarehouseReceiptDetails.Select(d => new ExportTransactionDetail
                {
                    WarehouseProductId = d.WarehouseProductId,
                    ProductId = d.ProductId,
                    Quantity = d.Quantity,
                    UnitPrice = d.UnitPrice,
                    TotalProductAmount = d.TotalProductAmount,
                    ExpiryDate = d.ExpiryDate
                }).ToList()
            };

            _context.ExportTransactions.Add(exportTransaction);

            await _context.SaveChangesAsync();
        }


        public async Task<List<ExportWarehouseReceipt>> GetAllReceiptsByWarehouseIdAsync(long warehouseId, string? sortBy = null)
        {
            var receipts = await _repository.GetAllReceiptsByWarehouseIdAsync(warehouseId);

            if (!string.IsNullOrEmpty(sortBy))
            {
                switch (sortBy.ToLower())
                {
                    case "status":
                        receipts = receipts
                            .OrderBy(r => r.Status == "Approved" ? 1 : 0) // ✅ Pending trước, Approved sau
                            .ToList();
                        break;
                    case "exportdate_desc":
                        receipts = receipts.OrderByDescending(r => r.ExportDate).ToList();
                        break;
                    case "exportdate_asc":
                        receipts = receipts.OrderBy(r => r.ExportDate).ToList();
                        break;
                }
            }

            return receipts;
        }

        public async Task<ExportWarehouseReceipt> CreateFromRequestAsync(int requestExportId, long warehouseId, Guid approvedBy)
        {
            if (await _repository.ReceiptExistsAsync(requestExportId))
                throw new InvalidOperationException($"RequestExportId = {requestExportId} has already been used to create an export receipt.");

            var requestExport = await _repository.GetRequestExportWithDetailsAsync(requestExportId);
            if (requestExport == null)
                throw new KeyNotFoundException("RequestExport not found");

            if (!requestExport.RequestExportDetails.Any())
                throw new InvalidOperationException("RequestExport has no details");

            var receipt = new ExportWarehouseReceipt
            {
                DocumentNumber = $"EXP-{DateTime.Now:yyyyMMddHHmmss}",
                DocumentDate = DateTime.Now,
                ExportDate = DateTime.Now,
                ExportType = "Xuất Bán",
                TotalQuantity = 0,
                TotalAmount = 0,
                RequestExportId = requestExportId,
                AgencyName = requestExport.Order?.RequestProduct?.AgencyAccount?.AgencyName,
                OrderCode = requestExport.Order?.OrderCode,
                WarehouseId = warehouseId,
                Status = "Pending",
                ExportWarehouseReceiptDetails = new List<ExportWarehouseReceiptDetail>()
            };

            decimal totalAmount = 0;
            int totalQuantity = 0;
            bool isPartial = false;

            foreach (var detail in requestExport.RequestExportDetails)
            {
                int quantityNeeded = detail.RequestedQuantity;
                int quantityExported = 0;

                var availableProducts = await _repository.GetAvailableWarehouseProductsAsync(detail.ProductId, warehouseId);

                foreach (var wp in availableProducts)
                {
                    if (quantityNeeded <= 0) break;

                    int quantityToExport = Math.Min(quantityNeeded, wp.Quantity);
                    var unitPrice = wp.Batch.SellingPrice ?? 0;
                    var amount = quantityToExport * unitPrice;

                    receipt.ExportWarehouseReceiptDetails.Add(new ExportWarehouseReceiptDetail
                    {
                        WarehouseProductId = wp.WarehouseProductId,
                        ProductId = detail.ProductId,
                        ProductName = wp.Product.ProductName,
                        BatchNumber = wp.Batch.BatchCode,
                        Quantity = quantityToExport,
                        UnitPrice = unitPrice,
                        TotalProductAmount = amount,
                        ExpiryDate = wp.Batch.ExpiryDate
                    });

                    quantityNeeded -= quantityToExport;
                    quantityExported += quantityToExport;
                    totalAmount += amount;
                    totalQuantity += quantityToExport;
                }

                // Đánh dấu nếu không đủ hàng
                if (quantityNeeded > 0)
                {
                    isPartial = true;
                }
            }

            if (receipt.ExportWarehouseReceiptDetails.Count == 0)
                throw new InvalidOperationException("Không có sản phẩm nào đủ tồn kho để tạo phiếu xuất.");

            receipt.TotalAmount = totalAmount;
            receipt.TotalQuantity = totalQuantity;
            receipt.Status = "Pending";

            await _repository.AddExportReceiptAsync(receipt);

            // ✅ Duyệt luôn phiếu vừa tạo
            await ApproveReceiptAsync(receipt.ExportWarehouseReceiptId, approvedBy);

            // ✅ Cập nhật trạng thái RequestExport nếu thiếu hàng
            if (isPartial)
            {
                var reqExport = await _context.RequestExports.FirstOrDefaultAsync(x => x.RequestExportId == requestExportId);
                if (reqExport != null)
                {
                    reqExport.Status = "Partially_Exported";
                    _context.RequestExports.Update(reqExport);
                    await _context.SaveChangesAsync();
                }
            }

            return receipt;
        }


        public async Task<bool> UpdateExportReceiptAsync(UpdateExportWarehouseReceiptFullDto dto)
        {
            return await _repository.UpdateFullAsync(dto);
        }

        public async Task<ExportWarehouseReceipt> CreateReceiptFromTransferAsync(ExportWarehouseTransferDTO dto, Guid userId)
        {
            var transferRequest = await _context.WarehouseTransferRequests
                .Include(r => r.TransferProducts)
                .FirstOrDefaultAsync(r => r.Id == dto.WarehouseTransferRequestId);

            if (transferRequest == null)
                throw new Exception("Không tìm thấy yêu cầu điều phối.");

            if (!transferRequest.TransferProducts.Any())
                throw new Exception("Yêu cầu điều phối không có sản phẩm.");

            if (transferRequest.SourceWarehouseId == null)
                throw new Exception("Yêu cầu điều phối chưa được chỉ định kho nguồn.");

            var sourceWarehouseId = transferRequest.SourceWarehouseId.Value;

            // ✅ Tạo phiếu xuất
            var receipt = new ExportWarehouseReceipt
            {
                DocumentNumber = $"EXP-TRANS-{DateTime.Now:yyyyMMddHHmmss}",
                DocumentDate = DateTime.Now,
                ExportDate = DateTime.Now,
                ExportType = "Xuất Điều Phối",
                WarehouseId = sourceWarehouseId,
                RequestExportId = transferRequest.RequestExportId,
                //OrderCode = transferRequest.OrderCode,
                AgencyName = null,
                Status = "Pending",
                ExportWarehouseReceiptDetails = new List<ExportWarehouseReceiptDetail>(),
                TotalQuantity = 0,
                TotalAmount = 0,
                WarehouseTransferRequestId = dto.WarehouseTransferRequestId
            };

            int totalQty = 0;
            decimal totalAmount = 0;

            foreach (var item in transferRequest.TransferProducts)
            {
                var wp = await _context.WarehouseProduct
                    .Include(p => p.Product)
                    .Include(p => p.Batch)
                    .FirstOrDefaultAsync(w => w.ProductId == item.ProductId &&
                                              w.WarehouseId == sourceWarehouseId &&
                                              w.Quantity >= item.Quantity);

                if (wp == null)
                    throw new Exception($"Không đủ tồn kho cho sản phẩm ID: {item.ProductId}");

                var unitPrice = wp.Batch?.SellingPrice ?? 0;
                var amount = unitPrice * item.Quantity;

                receipt.ExportWarehouseReceiptDetails.Add(new ExportWarehouseReceiptDetail
                {
                    WarehouseProductId = wp.WarehouseProductId,
                    ProductId = item.ProductId,
                    ProductName = wp.Product.ProductName,
                    BatchNumber = wp.Batch.BatchCode,
                    Quantity = item.Quantity,
                    UnitPrice = unitPrice,
                    TotalProductAmount = amount,
                    ExpiryDate = wp.Batch.ExpiryDate
                });

                totalQty += item.Quantity;
                totalAmount += amount;
            }

            receipt.TotalQuantity = totalQty;
            receipt.TotalAmount = totalAmount;

            // ✅ Lưu phiếu xuất
            await _repository.AddExportReceiptAsync(receipt);

            // ✅ Cập nhật trạng thái điều phối
            transferRequest.Status = "Approved";
            //transferRequest.ApprovedBy = userId;
            await _transferRequestRepo.UpdateAsync(transferRequest);

            // ✅ Duyệt luôn phiếu
            await ApproveReceiptAsync(receipt.ExportWarehouseReceiptId, userId);

            return receipt;
        }


        public async Task<byte[]> ExportReceiptToPdfAsync(long id)
        {
            var receipt = await _repository.GetReceiptByIdAsync(id);
            if (receipt == null)
                throw new Exception("Không tìm thấy phiếu xuất kho.");

            var html = new StringBuilder();
            html.AppendLine("<html><head><style>");
            html.AppendLine("body { font-family: Arial; font-size: 14px; }");
            html.AppendLine("table, th, td { border: 1px solid black; border-collapse: collapse; padding: 5px; }");
            html.AppendLine("th { background-color: #f2f2f2; }");
            html.AppendLine("</style></head><body>");

            html.AppendLine("<h2 style='text-align:center;'>PHIẾU XUẤT KHO</h2>");
            html.AppendLine($"<p><b>Số chứng từ:</b> {receipt.DocumentNumber}</p>");
            html.AppendLine($"<p><b>Ngày chứng từ:</b> {receipt.DocumentDate:dd/MM/yyyy}</p>");
            html.AppendLine($"<p><b>Ngày xuất:</b> {receipt.ExportDate:dd/MM/yyyy}</p>");
            html.AppendLine($"<p><b>Loại xuất:</b> {receipt.ExportType}</p>");
            html.AppendLine($"<p><b>Kho:</b> {receipt.WarehouseId}</p>");
            html.AppendLine($"<p><b>Đơn hàng:</b> {receipt.OrderCode}</p>");
            html.AppendLine($"<p><b>Đại lý:</b> {receipt.AgencyName}</p>");

            html.AppendLine("<h4>Danh sách sản phẩm xuất</h4>");
            html.AppendLine("<table width='100%'>");
            html.AppendLine("<tr><th>STT</th><th>Mã lô</th><th>Sản phẩm</th><th>SL</th><th>Đơn giá</th><th>Thành tiền</th><th>HSD</th></tr>");

            int stt = 1;
            foreach (var item in receipt.ExportWarehouseReceiptDetails)
            {
                html.AppendLine("<tr>");
                html.AppendLine($"<td>{stt++}</td>");
                html.AppendLine($"<td>{item.BatchNumber}</td>");
                html.AppendLine($"<td>{item.ProductName}</td>");
                html.AppendLine($"<td>{item.Quantity}</td>");
                html.AppendLine($"<td>{item.UnitPrice:N0}</td>");
                html.AppendLine($"<td>{item.TotalProductAmount:N0}</td>");
                html.AppendLine($"<td>{item.ExpiryDate:dd/MM/yyyy}</td>");
                html.AppendLine("</tr>");
            }

            html.AppendLine("</table>");
            html.AppendLine($"<p><b>Tổng số lượng:</b> {receipt.TotalQuantity}</p>");
            html.AppendLine($"<p><b>Tổng tiền:</b> {receipt.TotalAmount:N0} VND</p>");

            html.AppendLine("<br/><br/><table width='100%'><tr>");
            html.AppendLine("<td style='text-align:center;'>Người lập phiếu<br/><br/><br/>[Ký tên]</td>");
            html.AppendLine("<td style='text-align:center;'>Người duyệt<br/><br/><br/>[Ký tên]</td>");
            html.AppendLine("</tr></table>");

            html.AppendLine("</body></html>");

            return _pdfService.GeneratePdf(html.ToString());
        }


    }

}
