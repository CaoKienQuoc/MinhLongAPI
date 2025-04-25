using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessObject.DTO.Product;
using BusinessObject.DTO.ReturnOrder;
using BusinessObject.Models;
using Microsoft.AspNetCore.Http;
using Repo.IRepository;
using Repo.Repository;
using Services.IService;

namespace Services.Service
{
    public class ReturnService : IReturnService
    {
        private readonly IReturnRequestRepository _returnRepo;
        private readonly IDamagedStockRepository _damagedRepo;
        private readonly IWarehouseRepository _warehouseRepo;
        private readonly IImageService _imageService;
        private readonly IOrderRepository _orderRepo;
        private readonly IReturnWarehouseReceiptRepository _warehouseReceiptRepo;
        private readonly IWarehouseExportRepository _warehouseExportRepo;

        public ReturnService(
            IReturnRequestRepository returnRepo,
            IDamagedStockRepository damagedRepo,
            IWarehouseRepository warehouseRepo,
            IOrderRepository orderRepo,
            IImageService imageService,
            IReturnWarehouseReceiptRepository warehouseReceiptRepo,
            IWarehouseExportRepository warehouseExportRepo)
        {
            _returnRepo = returnRepo;
            _damagedRepo = damagedRepo;
            _warehouseRepo = warehouseRepo;
            _orderRepo = orderRepo;
            _imageService = imageService;
            _warehouseReceiptRepo = warehouseReceiptRepo;
            _warehouseExportRepo = warehouseExportRepo;
        }

        public async Task<ReturnRequest> CreateReturnRequestWithImagesAsync(Guid orderId, Guid orderDetailId, int quantity, string reason, string? note, Guid userId, List<IFormFile> images)
        {

            // 🔎 Lấy đơn hàng và kiểm tra trạng thái
            var order = await _orderRepo.GetOrderByIdAsync(orderId);
            if (order == null)
                throw new Exception("Không tìm thấy đơn hàng.");

            if (!string.Equals(order.Status, "Exported", StringComparison.OrdinalIgnoreCase))
                throw new Exception("Chỉ được phép tạo yêu cầu trả hàng cho đơn hàng đã xuất.");

            var orderDetail = await _orderRepo.GetOrderDetailByIdAsync(orderDetailId);
            if (orderDetail == null) throw new Exception("Không tìm thấy OrderDetail.");

            long productId = orderDetail.ProductId;

            // Upload ảnh lên Cloudinary hoặc thư mục lưu trữ
            var imageModel = new ImageModel { Files = images };
            var uploadedImages = await _imageService.UploadImagesAsync(imageModel, productId);

            // Tạo return request
            var returnRequest = new ReturnRequest
            {
                OrderId = orderId,
                CreatedByUserId = userId,
                Note = note,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow,
                Details = new List<ReturnRequestDetail>
            {
                new ReturnRequestDetail
                {
                    OrderDetailId = orderDetailId,
                    ProductId = productId,
                    QuantityReturned = quantity,
                    Reason = reason
                }
            }
            };

            var savedRequest = await _returnRepo.CreateAsync(returnRequest);

            var detailId = savedRequest.Details.First().ReturnRequestDetailId;

            var returnImages = uploadedImages.Select(img => new ReturnRequestImage
            {
                ReturnRequestDetailId = detailId,
                ImageUrl = img.ImageUrl,
                UploadedAt = DateTime.UtcNow
            }).ToList();

            await _returnRepo.AddRangeAsync(returnImages);

            return savedRequest;
        }


        public async Task ApproveReturnRequestAsync(Guid returnRequestId)
        {
            // 🔍 Lấy dữ liệu yêu cầu trả hàng + chi tiết
            var request = await _returnRepo.GetByIdWithDetailsAsync(returnRequestId);
            if (request == null)
                throw new Exception("Không tìm thấy yêu cầu trả hàng.");

            if (!string.Equals(request.Status, "Pending", StringComparison.OrdinalIgnoreCase))
                throw new Exception("Yêu cầu đã được duyệt trước đó rồi!");

            // 🔍 Tìm warehouseId từ Order
            var warehouseId = await _warehouseExportRepo.GetWarehouseIdFromOrderAsync(request.OrderId);
            if (warehouseId == null)
                throw new Exception("Không tìm thấy kho xuất ban đầu từ đơn hàng.");

            // 🔍 Lấy phiếu xuất kho bán
            var exportReceipt = await _warehouseExportRepo.GetExportSaleByOrderIdAsync(request.OrderId);
            if (exportReceipt == null)
                throw new Exception("Không tìm thấy phiếu xuất kho bán tương ứng.");

            // ✅ Cập nhật trạng thái yêu cầu trả hàng
            request.Status = "Approved";

            // ✅ Tạo phiếu nhập kho hủy
            var receipt = new ReturnWarehouseReceipt
            {
                ReturnRequestId = request.ReturnRequestId,
                ReceiptCode = $"RR-{DateTime.UtcNow:yyyyMMddHHmmss}",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = request.CreatedByUserId,
                WarehouseId = warehouseId,
                Note = request.Note,
                ReceiptDate = DateTime.UtcNow,
                Status = "Pending"
            };

            // ✅ Lưu phiếu trước để lấy ID
            await _warehouseReceiptRepo.CreateAsync(receipt);
            await _warehouseReceiptRepo.SaveChangesAsync();

            // ✅ Mapping từng chi tiết
            var receiptDetails = request.Details.Select(detail =>
            {
                var exportDetail = exportReceipt.ExportWarehouseReceiptDetails
                    .FirstOrDefault(ed => ed.ProductId == detail.ProductId);

                if (exportDetail == null)
                    throw new Exception($"Không tìm thấy Batch cho sản phẩm ID: {detail.ProductId}");

                return new ReturnWarehouseReceiptDetail
                {
                    ReturnWarehouseReceiptId = receipt.ReturnWarehouseReceiptId,
                    ProductId = detail.ProductId,
                    Quantity = detail.QuantityReturned,
                    Reason = detail.Reason,
                    BatchId = exportDetail.BatchId,
                };
            }).ToList();

            // ✅ Lưu chi tiết phiếu
            await _warehouseReceiptRepo.CreateReturnWarehouseReceiptDetailAsync(receiptDetails);
            await _warehouseReceiptRepo.SaveChangesAsync();
        }



        public async Task ImportToDamagedStockAsync(Guid returnRequestId, Guid warehouseUserId)
        {
            var request = await _returnRepo.GetByIdWithDetailsAsync(returnRequestId);
            if (request == null)
                throw new Exception("Không tìm thấy yêu cầu trả hàng.");

            if (request.Status != "Approved")
                throw new Exception("Yêu cầu chưa được duyệt.");

            var warehouseId = await _warehouseRepo.GetWarehouseIdByUserAsync(warehouseUserId);
            if (warehouseId == 0)
                throw new Exception("Không tìm thấy kho tương ứng với người dùng.");

            var damagedStocks = new List<DamagedStock>();

            foreach (var item in request.Details)
            {
                if (item.OrderDetail == null)
                    throw new Exception("Thiếu thông tin sản phẩm trong đơn hàng.");

                damagedStocks.Add(new DamagedStock
                {
                    ProductId = item.OrderDetail.ProductId,
                    WarehouseId = warehouseId,
                    Quantity = item.QuantityReturned,
                    BatchCode = $"DAM-{DateTime.UtcNow:yyyyMMddHHmmss}"
                });
            }

            await _damagedRepo.AddRangeAsync(damagedStocks);
            await _returnRepo.UpdateStatusAsync(returnRequestId, "Imported");
        }

        public async Task<List<ReturnRequest>> GetPendingReturnsAsync()
        {
            return await _returnRepo.GetPendingApprovalAsync();
        }

    }

}
