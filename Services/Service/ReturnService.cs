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
        private readonly IReturnWarehouseReceiptRepository _returnWarehouseReceiptRepo;
        private readonly IUserRepository _employeeRepo;

        public ReturnService(
            IReturnRequestRepository returnRepo,
            IDamagedStockRepository damagedRepo,
            IWarehouseRepository warehouseRepo,
            IOrderRepository orderRepo,
            IImageService imageService,
            IReturnWarehouseReceiptRepository warehouseReceiptRepo,
            IWarehouseExportRepository warehouseExportRepo,
            IReturnWarehouseReceiptRepository returnWarehouseReceiptRepo,
            IUserRepository employeeRepo)
        {
            _returnRepo = returnRepo;
            _damagedRepo = damagedRepo;
            _warehouseRepo = warehouseRepo;
            _orderRepo = orderRepo;
            _imageService = imageService;
            _warehouseReceiptRepo = warehouseReceiptRepo;
            _warehouseExportRepo = warehouseExportRepo;
            _returnWarehouseReceiptRepo = returnWarehouseReceiptRepo;
            _employeeRepo = employeeRepo;
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

            // 🔥 Validate số lượng trả
            // 1. Tổng số lượng đã trả trước đó
            int totalReturned = await _returnRepo.GetTotalReturnedQuantityAsync(orderDetailId);

            // 2. Số lượng còn lại có thể trả
            int availableQuantity = orderDetail.Quantity - totalReturned;

            if (quantity > availableQuantity)
                throw new Exception($"Số lượng trả vượt quá số lượng còn lại. Số lượng còn lại có thể trả là {availableQuantity} sản phẩm.");


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


        public async Task ApproveReturnRequestAsync(Guid returnRequestId, Guid userId)
        {
            // —————— 0) Validate user là Sale (Employee) ——————
            if (!await _employeeRepo.ExistsAsync(userId))
                throw new UnauthorizedAccessException("Bạn không phải Sale.");
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
                ApprovedBy = userId,
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
        

        public async Task<List<ReturnRequest>> GetPendingReturnsAsync()
        {
            return await _returnRepo.GetPendingApprovalAsync();
        }


        public async Task<List<ReturnRequestProdductDto>> GetAllReturnRequestsAsync()
        {
            var requests = await _returnRepo.GetAllAsync();

            return requests.Select(r => new ReturnRequestProdductDto
            {
                ReturnRequestId = r.ReturnRequestId,
                OrderId = r.OrderId,
                CreatedAt = r.CreatedAt,
                CreatedByUserName = r.Order.RequestProduct.AgencyAccount.User.Username,
                Status = r.Status,
                Note = r.Note,
                Details = r.Details?.Select(d => new ReturnRequestProdductDetailDto
                {
                    ReturnRequestDetailId = d.ReturnRequestDetailId,
                    OrderDetailId = d.OrderDetailId,
                    ProductName = d.Product.ProductName,
                    Reason = d.Reason,
                    QuantityReturned = d.QuantityReturned,
                    Images = d.Images?.Select(img => new ReturnRequestImageDto
                    {
                        ReturnRequestImageId = img.ReturnRequestImageId,
                        ImageUrl = img.ImageUrl
                    }).ToList()
                }).ToList()
            }).ToList();
        }

        public async Task<ReturnRequestProdductDto> GetReturnRequestByIdAsync(Guid id)
        {
            var r = await _returnRepo.GetByIdWithAllDetailsAsync(id);
            if (r == null) return null;

            return new ReturnRequestProdductDto
            {
                ReturnRequestId = r.ReturnRequestId,
                OrderId = r.OrderId,
                CreatedAt = r.CreatedAt,
                CreatedByUserName = r.Order.RequestProduct.AgencyAccount.User.Username,
                Status = r.Status,
                Note = r.Note,
                Details = r.Details?.Select(d => new ReturnRequestProdductDetailDto
                {
                    ReturnRequestDetailId = d.ReturnRequestDetailId,
                    OrderDetailId = d.OrderDetailId,
                    ProductName = d.Product.ProductName,
                    Reason = d.Reason,
                    QuantityReturned = d.QuantityReturned,
                    Images = d.Images?.Select(img => new ReturnRequestImageDto
                    {
                        ReturnRequestImageId = img.ReturnRequestImageId,
                        ImageUrl = img.ImageUrl
                    }).ToList()
                }).ToList()
            };
        }


        public async Task<List<ReturnRequestProdductDto>> GetApprovedReturnRequestsAsync()
        {
            var requests = await _returnRepo.GetApprovedAsync();

            return requests.Select(r => new ReturnRequestProdductDto
            {
                ReturnRequestId = r.ReturnRequestId,
                OrderId = r.OrderId,
                CreatedAt = r.CreatedAt,
                CreatedByUserName = r.Order.RequestProduct.AgencyAccount.User.Username,
                Status = r.Status,
                Note = r.Note,
                Details = r.Details?.Select(d => new ReturnRequestProdductDetailDto
                {
                    ReturnRequestDetailId = d.ReturnRequestDetailId,
                    OrderDetailId = d.OrderDetailId,
                    ProductName = d.Product.ProductName,
                    Reason = d.Reason,
                    QuantityReturned = d.QuantityReturned,
                    Images = d.Images?.Select(img => new ReturnRequestImageDto
                    {
                        ReturnRequestImageId = img.ReturnRequestImageId,
                        ImageUrl = img.ImageUrl
                    }).ToList()
                }).ToList()
            }).ToList();
        }

        public async Task<ReturnRequestProdductDto> GetApprovedReturnRequestByIdAsync(Guid id)
        {
            var r = await _returnRepo.GetApprovedByIdAsync(id);
            if (r == null) return null;

            return new ReturnRequestProdductDto
            {
                ReturnRequestId = r.ReturnRequestId,
                OrderId = r.OrderId,
                CreatedAt = r.CreatedAt,
                CreatedByUserName = r.Order.RequestProduct.AgencyAccount.User.Username,
                Status = r.Status,
                Note = r.Note,
                Details = r.Details?.Select(d => new ReturnRequestProdductDetailDto
                {
                    ReturnRequestDetailId = d.ReturnRequestDetailId,
                    OrderDetailId = d.OrderDetailId,
                    ProductName = d.Product.ProductName,
                    Reason = d.Reason,
                    QuantityReturned = d.QuantityReturned,
                    Images = d.Images?.Select(img => new ReturnRequestImageDto
                    {
                        ReturnRequestImageId = img.ReturnRequestImageId,
                        ImageUrl = img.ImageUrl
                    }).ToList()
                }).ToList()
            };
        }


        public async Task<List<ReturnWarehouseReceiptDto>> GetAllReturnWarehouseReceiptsAsync()
        {
            var receipts = await _warehouseReceiptRepo.GetAllAsync();

            return receipts.Select(r => new ReturnWarehouseReceiptDto
            {
                ReturnWarehouseReceiptId = r.ReturnWarehouseReceiptId,
                ReceiptCode = r.ReceiptCode,
                ReceiptDate = r.ReceiptDate,
                CreatedAt = r.CreatedAt,
                CreatedByUserName = r.ReturnRequest.Order.RequestProduct.AgencyAccount.User.Username,
                ReturnRequestId = r.ReturnRequestId,
                WarehouseId = r.WarehouseId,
                Note = r.Note,
                Status = r.Status,
                Details = r.Details?.Select(d => new ReturnWarehouseReceiptDetailDto
                {
                    ReturnWarehouseReceiptDetailId = d.ReturnWarehouseReceiptDetailId,
                    ProductName = d.Product.ProductName,
                    Quantity = d.Quantity,
                    BatchId = d.BatchId,
                    Reason = d.Reason
                }).ToList()
            }).ToList();
        }

        public async Task<ReturnWarehouseReceiptDto> GetReturnWarehouseReceiptByIdAsync(long id)
        {
            var r = await _warehouseReceiptRepo.GetByIdWithDetailsAsync(id);
            if (r == null) return null;

            return new ReturnWarehouseReceiptDto
            {
                ReturnWarehouseReceiptId = r.ReturnWarehouseReceiptId,
                ReceiptCode = r.ReceiptCode,
                ReceiptDate = r.ReceiptDate,
                CreatedAt = r.CreatedAt,
                CreatedByUserName = r.ReturnRequest.Order.RequestProduct.AgencyAccount.User.Username,
                ReturnRequestId = r.ReturnRequestId,
                WarehouseId = r.WarehouseId,
                Note = r.Note,
                Status = r.Status,
                Details = r.Details?.Select(d => new ReturnWarehouseReceiptDetailDto
                {
                    ReturnWarehouseReceiptDetailId = d.ReturnWarehouseReceiptDetailId,
                    ProductName = d.Product.ProductName,
                    Quantity = d.Quantity,
                    BatchId = d.BatchId,
                    Reason = d.Reason
                }).ToList()
            };
        }

        public async Task<IEnumerable<ReturnWarehouseReceiptDto>> GetByWarehouseIdAsync(long warehouseId)
        {
            var receipts = await _returnWarehouseReceiptRepo.GetByWarehouseIdAsync(warehouseId);

            var result = receipts.Select(r => new ReturnWarehouseReceiptDto
            {
                ReturnWarehouseReceiptId = r.ReturnWarehouseReceiptId,
                ReceiptCode = r.ReceiptCode,
                ReceiptDate = r.ReceiptDate,
                Note = r.Note,
                Status = r.Status,
                Details = r.Details?.Select(d => new ReturnWarehouseReceiptDetailDto
                {
                    ReturnWarehouseReceiptDetailId = d.ReturnWarehouseReceiptDetailId,
                    ProductName = d.Product.ProductName,
                    Quantity = d.Quantity,
                    BatchId = d.BatchId,
                    Reason = d.Reason
                }).ToList()
            });

            return result;
        }


    }

}
