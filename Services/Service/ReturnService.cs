using System;
using System.Collections.Generic;
using System.Globalization;
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

        private string NormalizeString(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;

            // Loại bỏ dấu tiếng Việt
            var normalizedString = input.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();

            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(c);
                }
            }

            // Loại bỏ khoảng trắng thừa và chuyển về chữ thường
            return sb.ToString().Replace(" ", "").ToLowerInvariant();
        }

        public async Task<ReturnRequest> CreateReturnRequestWithImagesAsync(Guid orderId, Guid orderDetailId, int quantity, string reason, Guid userId, List<IFormFile> images)
        {
            //string returnRequestCode = await _returnRepo.GenerateRequestReturnCodeAsync();
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
                throw new Exception($"Số lượng trả vượt quá số lượng còn lại. Số lượng còn lại có thể trả là {availableQuantity} sản phẩm cho đơn hàng này.");
            
            // 🔎 Kiểm tra nếu đã có ReturnRequest cho sản phẩm này
            var existingRequest = await _returnRepo.GetByOrderAndProductAsync(orderId, productId);
            // Biến dùng chung cho upload ảnh
            ImageModel imageModel = null;
            List<ReturnRequestImage> uploadedImages = null;

            // Nếu đã có ReturnRequest trước đó
            if (existingRequest != null)
            {
                // ✅ Tìm chi tiết trả hàng của sản phẩm này
                //var existingDetail = existingRequest.Details.FirstOrDefault(d => d.OrderDetailId == orderDetailId);

                // 🔍 Kiểm tra lý do đã tồn tại (không phân biệt dấu, khoảng trắng, chữ hoa/thường)
                var normalizedReason = NormalizeString(reason);
                var existingDetail = existingRequest.Details.FirstOrDefault(d =>
                    d.OrderDetailId == orderDetailId &&
                    d.ProductId == productId &&
                    NormalizeString(d.Reason) == normalizedReason
                );


                if (existingDetail != null)
                {
                    // Cập nhật số lượng trả hàng
                    existingDetail.QuantityReturned += quantity;
                    await _returnRepo.UpdateAsync(existingRequest);

                    // ✅ Upload thêm ảnh nếu có
                    if (images != null && images.Count > 0)
                    {
                        imageModel = new ImageModel { Files = images };
                        uploadedImages = await _imageService.UploadReturnImagesAsync(imageModel, existingDetail.ReturnRequestDetailId);

                        // Lưu ảnh mới vào database
                        //await _returnRepo.AddRangeAsync(uploadedImages);
                    }

                    return existingRequest;
                }
                else
                {
                    // ✅ Tạo chi tiết trả hàng mới nếu lý do khác
                    var newDetail = new ReturnRequestDetail
                    {
                        OrderDetailId = orderDetailId,
                        ProductId = productId,
                        QuantityReturned = quantity,
                        Reason = reason
                    };

                    existingRequest.Details.Add(newDetail);
                    await _returnRepo.UpdateAsync(existingRequest);
                    await _returnRepo.SaveChangesAsync();

                    // 🔄 Lấy chính xác ReturnRequestDetailId sau khi lưu
                    var savedDetail = existingRequest.Details.LastOrDefault(d =>
                        d.OrderDetailId == orderDetailId &&
                        d.ProductId == productId &&
                        d.Reason == reason
                    );

                    // ✅ Upload ảnh nếu có
                    if (images != null && images.Count > 0)
                    {
                        imageModel = new ImageModel { Files = images };
                        uploadedImages = await _imageService.UploadReturnImagesAsync(imageModel, savedDetail.ReturnRequestDetailId);
                        //await _returnRepo.AddRangeAsync(uploadedImages);
                    }

                    return existingRequest;
                }
            }

            // Tạo ReturnRequest mới nếu chưa có
            string returnRequestCode = await _returnRepo.GenerateRequestReturnCodeAsync();
            // Tạo return request
            var returnRequest = new ReturnRequest
            {
                OrderId = orderId,
                CreatedByUserId = userId,
                //Note = note,
                Status = "Pending",
                ReturnRequestCode = returnRequestCode,
                CreatedAt = GetVietnamTime(),
                
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

            /*// ✅ Lấy chính xác ReturnRequestDetailId sau khi lưu
            var returnDetailId = savedRequest.Details.First().ReturnRequestDetailId;

            // Upload ảnh lên Cloudinary hoặc thư mục lưu trữ
            var imageModel = new ImageModel { Files = images };
            var uploadedImages = await _imageService.UploadReturnImagesAsync(imageModel, returnDetailId);*/

            // ✅ Upload ảnh cho ReturnRequest mới
            if (images != null && images.Count > 0)
            {
                var returnDetailId = savedRequest.Details.First().ReturnRequestDetailId;
                imageModel = new ImageModel { Files = images };
                uploadedImages = await _imageService.UploadReturnImagesAsync(imageModel, returnDetailId);
                await _returnRepo.AddRangeAsync(uploadedImages);
            }

            return savedRequest;
        }
        public DateTime GetVietnamTime()
        {
            // Lấy múi giờ Việt Nam (GMT+7)
            TimeZoneInfo vietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            DateTime vietnamTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vietnamTimeZone);
            return vietnamTime;
        }

        public async Task ApproveReturnRequestAsync(Guid returnRequestId, Guid userId)
        {
            string returnWarehouseCode = await _returnRepo.GenerateWarehouseReturnCodeAsync();
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
                ReceiptCode = returnWarehouseCode,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = request.CreatedByUserId,
                ApprovedBy = userId,
                WarehouseId = warehouseId,
                //Note = request.Note,
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
            //request.Reason = "Đã duyệt yêu cầu trả hàng";
            await _warehouseReceiptRepo.SaveChangesAsync();
        }

        public async Task RejectReturnRequestAsync(Guid returnRequestId, Guid userId, string rejectReason)
        {
            if (!await _employeeRepo.ExistsAsync(userId))
                throw new UnauthorizedAccessException("Bạn không phải Sale.");

            var request = await _returnRepo.GetByIdWithDetailsAsync(returnRequestId);
            if (request == null)
                throw new Exception("Không tìm thấy yêu cầu trả hàng.");

            if (!string.Equals(request.Status, "Pending", StringComparison.OrdinalIgnoreCase))
                throw new Exception("Yêu cầu đã được xử lý trước đó!");

            request.Status = "Rejected";
            request.RejectedAt = DateTime.UtcNow;
            request.RejectedBy = userId;
            request.Reason = rejectReason;

            await _returnRepo.UpdateAsync(request);
            await _returnRepo.SaveChangesAsync();
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
                ReturnRequestCode = r.ReturnRequestCode,
                Status = r.Status,
                //Note = r.Note,
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
                ReturnRequestCode = r.ReturnRequestCode,
                Status = r.Status,
                //Note = r.Note,
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


        public async Task<List<ReturnRequestProdductDto>> GetAllReturnRequestsAsyncForSales(Guid userId)
        {
            var requests = await _returnRepo.GetAllAsync();

            // Lọc các đơn trả hàng có đại lý được quản lý bởi sale này
            var filtered = requests
                .Where(r => r.Order?.RequestProduct?.AgencyAccount?.ManagedByEmployee?.UserId == userId)
                .ToList();



            return filtered.Select(r => new ReturnRequestProdductDto
            {
                ReturnRequestId = r.ReturnRequestId,
                OrderId = r.OrderId,
                OrderCode = r.Order.OrderCode,
                CreatedAt = r.CreatedAt,
                CreatedByUserName = r.Order.RequestProduct.AgencyAccount.User.Username,
                ReturnRequestCode = r.ReturnRequestCode,
                Status = r.Status,
                //Note = r.Note,
                Details = r.Details?.Select(d => new ReturnRequestProdductDetailDto
                {
                    ReturnRequestDetailId = d.ReturnRequestDetailId,
                    OrderDetailId = d.OrderDetailId,
                    ProductName = d.Product?.ProductName ?? "",
                    Reason = d.Reason,
                    QuantityReturned = d.QuantityReturned,
                    Images = d.Images?.Select(img => new ReturnRequestImageDto
                    {
                        ReturnRequestImageId = img.ReturnRequestImageId,
                        ImageUrl = img.ImageUrl
                    }).ToList() ?? new List<ReturnRequestImageDto>()
                }).ToList() ?? new List<ReturnRequestProdductDetailDto>()
            }).ToList();
        }

        public async Task<ReturnRequestProdductDto> GetReturnRequestByIdAsyncForSales(Guid id, Guid userId)
        {
            var r = await _returnRepo.GetByIdWithAllDetailsAsync(id);
            if (r == null) return null;

            // Kiểm tra xem đơn có thuộc agency do sale quản lý không
            if (r.Order?.RequestProduct?.AgencyAccount?.ManagedByEmployee?.UserId != userId)
                throw new UnauthorizedAccessException("Bạn không có quyền truy cập đơn này.");

            return new ReturnRequestProdductDto
            {
                ReturnRequestId = r.ReturnRequestId,
                OrderId = r.OrderId,
                OrderCode = r.Order.OrderCode,
                CreatedAt = r.CreatedAt,
                CreatedByUserName = r.Order.RequestProduct.AgencyAccount.User.Username,
                ReturnRequestCode = r.ReturnRequestCode,
                Status = r.Status,
                //Note = r.Note,
                Details = r.Details?.Select(d => new ReturnRequestProdductDetailDto
                {
                    ReturnRequestDetailId = d.ReturnRequestDetailId,
                    OrderDetailId = d.OrderDetailId,
                    ProductName = d.Product?.ProductName ?? "",
                    Reason = d.Reason,
                    QuantityReturned = d.QuantityReturned,
                    Images = d.Images?.Select(img => new ReturnRequestImageDto
                    {
                        ReturnRequestImageId = img.ReturnRequestImageId,
                        ImageUrl = img.ImageUrl
                    }).ToList() ?? new List<ReturnRequestImageDto>()
                }).ToList() ?? new List<ReturnRequestProdductDetailDto>()
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
                ReturnRequestCode = r.ReturnRequestCode,
                Status = r.Status,
                //Note = r.Note,
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
                ReturnRequestCode = r.ReturnRequestCode,
                //Note = r.Note,
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
                ReturnRequestCode = r.ReturnRequest.ReturnRequestCode,
                WarehouseId = r.WarehouseId,
                //Note = r.Note,
                Status = r.Status,
                Details = r.Details?.Select(d =>
                {
                    // 🔗 Kết nối từ ReturnRequestDetail để lấy hình ảnh
                    var relatedRequestDetail = r.ReturnRequest.Details
                        .FirstOrDefault(reqDetail => reqDetail.ProductId == d.ProductId);

                    return new ReturnWarehouseReceiptDetailDto
                    {
                        ReturnWarehouseReceiptDetailId = d.ReturnWarehouseReceiptDetailId,
                        ProductName = d.Product.ProductName,
                        Quantity = d.Quantity,
                        BatchId = d.BatchId,
                        Reason = d.Reason,
                        Images = relatedRequestDetail?.Images?.Select(img => new ReturnRequestImageDto
                        {
                            ReturnRequestImageId = img.ReturnRequestImageId,
                            ImageUrl = img.ImageUrl
                        }).ToList() ?? new List<ReturnRequestImageDto>()
                    };
                }).ToList() ?? new List<ReturnWarehouseReceiptDetailDto>()

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
                ReturnRequestCode = r.ReturnRequest.ReturnRequestCode,
                WarehouseId = r.WarehouseId,
                //Note = r.Note,
                Status = r.Status,
                Details = r.Details?.Select(d =>
                {
                    // 🔗 Kết nối từ ReturnRequestDetail để lấy hình ảnh
                    var relatedRequestDetail = r.ReturnRequest.Details
                        .FirstOrDefault(reqDetail => reqDetail.ProductId == d.ProductId);

                    return new ReturnWarehouseReceiptDetailDto
                    {
                        ReturnWarehouseReceiptDetailId = d.ReturnWarehouseReceiptDetailId,
                        ProductName = d.Product.ProductName,
                        Quantity = d.Quantity,
                        BatchId = d.BatchId,
                        Reason = d.Reason,
                        Images = relatedRequestDetail?.Images?.Select(img => new ReturnRequestImageDto
                        {
                            ReturnRequestImageId = img.ReturnRequestImageId,
                            ImageUrl = img.ImageUrl
                        }).ToList() ?? new List<ReturnRequestImageDto>()
                    };
                }).ToList() ?? new List<ReturnWarehouseReceiptDetailDto>()
            };
        }

        public async Task<IEnumerable<ReturnWarehouseReceiptDto>> GetByWarehouseIdAsync(long warehouseId)
        {
            var receipts = await _returnWarehouseReceiptRepo.GetByWarehouseIdAsync(warehouseId);

            if (receipts == null || !receipts.Any())
            {
                return new List<ReturnWarehouseReceiptDto>(); // ✅ trả mảng rỗng nếu không có dữ liệu
            }

            var result = receipts.Select(r => new ReturnWarehouseReceiptDto
            {
                ReturnWarehouseReceiptId = r.ReturnWarehouseReceiptId,
                ReturnRequestId = r.ReturnRequestId,
                ReturnRequestCode = r.ReturnRequest?.ReturnRequestCode, // 🔥 dùng ? để tránh lỗi nếu ReturnRequest null
                ReceiptCode = r.ReceiptCode,
                ReceiptDate = r.ReceiptDate,
                WarehouseId = r.WarehouseId,
                //Note = r.Note,
                Status = r.Status,
                Details = r.Details?.Select(d =>
                {
                    // 🔗 Kết nối từ ReturnRequestDetail để lấy hình ảnh
                    var relatedRequestDetail = r.ReturnRequest.Details
                        .FirstOrDefault(reqDetail => reqDetail.ProductId == d.ProductId);

                    return new ReturnWarehouseReceiptDetailDto
                    {
                        ReturnWarehouseReceiptDetailId = d.ReturnWarehouseReceiptDetailId,
                        ProductName = d.Product.ProductName,
                        Quantity = d.Quantity,
                        BatchId = d.BatchId,
                        Reason = d.Reason,
                        Images = relatedRequestDetail?.Images?.Select(img => new ReturnRequestImageDto
                        {
                            ReturnRequestImageId = img.ReturnRequestImageId,
                            ImageUrl = img.ImageUrl
                        }).ToList() ?? new List<ReturnRequestImageDto>()
                    };
                }).ToList() ?? new List<ReturnWarehouseReceiptDetailDto>()
            }).ToList();

            return result;
        }

        public async Task<IEnumerable<ReturnRequestProdductDto>> GetReturnRequestsByUserIdAsync(Guid userId)
        {
            var requests = await _returnRepo.GetByUserIdAsync(userId) ?? new List<ReturnRequest>(); ;
            var user = await _employeeRepo.GetByIdAsync(userId);

            return requests.Select(r => new ReturnRequestProdductDto
            {
                ReturnRequestId = r.ReturnRequestId,
                OrderId = r.OrderId,
                OrderCode = r.Order.OrderCode,
                CreatedAt = r.CreatedAt,
                CreatedByUserName = user?.Username ?? "Unknown",
                Status = r.Status,
                ReturnRequestCode = r.ReturnRequestCode,
                //Note = r.Note,
                Details = r.Details?.Select(d => new ReturnRequestProdductDetailDto
                {
                    ReturnRequestDetailId = d.ReturnRequestDetailId,
                    OrderDetailId = d.OrderDetailId,
                    ProductName = d.Product?.ProductName ?? "",
                    Reason = d.Reason,
                    QuantityReturned = d.QuantityReturned,
                    Images = d.Images?.Select(img => new ReturnRequestImageDto
                    {
                        ReturnRequestImageId = img.ReturnRequestImageId,
                        ImageUrl = img.ImageUrl
                    }).ToList() ?? new List<ReturnRequestImageDto>()
                }).ToList() ?? new List<ReturnRequestProdductDetailDto>()
            }).ToList();
        }


        public async Task<ReturnRequestProdductDto> GetReturnRequestByIdAsync(Guid returnRequestId, Guid userId)
        {
            var request = await _returnRepo.GetByIdAndUserIdAsync(returnRequestId, userId);
            if (request == null) return null;

            var user = await _employeeRepo.GetByIdAsync(userId);

            return new ReturnRequestProdductDto
            {
                ReturnRequestId = request.ReturnRequestId,
                OrderId = request.OrderId,
                OrderCode = request.Order.OrderCode,
                CreatedAt = request.CreatedAt,
                CreatedByUserName = user?.Username ?? "Unknown",
                Status = request.Status,
                ReturnRequestCode = request.ReturnRequestCode,
                //Note = request.Note,
                Details = request.Details?.Select(d => new ReturnRequestProdductDetailDto
                {
                    ReturnRequestDetailId = d.ReturnRequestDetailId,
                    OrderDetailId = d.OrderDetailId,
                    ProductName = d.Product?.ProductName ?? "",
                    Reason = d.Reason,
                    QuantityReturned = d.QuantityReturned,
                    Images = d.Images?.Select(img => new ReturnRequestImageDto
                    {
                        ReturnRequestImageId = img.ReturnRequestImageId,
                        ImageUrl = img.ImageUrl
                    }).ToList() ?? new List<ReturnRequestImageDto>()
                }).ToList() ?? new List<ReturnRequestProdductDetailDto>()
            };
        }


    }

}
