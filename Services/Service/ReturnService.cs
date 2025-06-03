using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessObject.DTO.Dashboard;
using BusinessObject.DTO.Product;
using BusinessObject.DTO.ReturnOrder;
using BusinessObject.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
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
        private readonly INotificationRepository _notificationRepository;
        private readonly IEmailService _emailService;
        private readonly IHubContext<NotificationHub> _hub;
        private readonly IOrderRepository _orderRepository;
        private readonly IRequestProductRepository _requestProductRepository;
        private readonly IUserRepository _userRepository;

        public ReturnService(
            IReturnRequestRepository returnRepo,
            IDamagedStockRepository damagedRepo,
            IWarehouseRepository warehouseRepo,
            IOrderRepository orderRepo,
            IImageService imageService,
            IReturnWarehouseReceiptRepository warehouseReceiptRepo,
            IWarehouseExportRepository warehouseExportRepo,
            IReturnWarehouseReceiptRepository returnWarehouseReceiptRepo,
            IUserRepository employeeRepo,
            IHubContext<NotificationHub> hub,
            INotificationRepository notificationRepository,
            IEmailService emailService,
            IOrderRepository orderRepository,
            IRequestProductRepository requestProductRepository,
            IUserRepository userRepository)
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
            _hub = hub;
            _notificationRepository = notificationRepository;
            _emailService = emailService;
            _orderRepository = orderRepository;
            _requestProductRepository = requestProductRepository;
            _userRepository = userRepository;
        }

        private string NormalizeString(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            // 1. Tách ký tự có dấu thành ký tự cơ bản + dấu
            var normalizedFormD = input.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (var c in normalizedFormD)
            {
                // 2. Bỏ mark (dấu)
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }

            // 3. Xóa hết khoảng trắng, chuyển chữ thường
            return sb
                .ToString()
                .Replace(" ", "")
                .ToLowerInvariant();
        }

        /*public async Task<ReturnRequest> CreateReturnRequestWithImagesAsync(
    Guid orderId,
    List<(Guid orderDetailId, int quantity, string reason)> itemDetails,
    Guid userId,
    List<IFormFile> images)
        {
            var order = await _orderRepo.GetOrderByIdAsync(orderId);
            if (order == null)
                throw new Exception("Không tìm thấy đơn hàng.");

            if (!string.Equals(order.Status, "Exported", StringComparison.OrdinalIgnoreCase))
                throw new Exception("Chỉ được phép tạo yêu cầu trả hàng cho đơn hàng đã xuất.");

            var exportReceipt = await _warehouseExportRepo.GetExportSaleByOrderIdAsync(orderId);
            if (exportReceipt == null)
                throw new Exception("Không tìm thấy phiếu xuất kho bán tương ứng.");

            // ✅ Kiểm tra nếu ngày xuất quá 30 ngày thì không cho tạo yêu cầu
            var exportDate = exportReceipt.ExportDate;
            var nowVN = GetVietnamTime();
            var daysDiff = (nowVN.Date - exportDate.Date).TotalDays;

            if (daysDiff > 30)
                throw new Exception("Đơn hàng đã được xuất quá 30 ngày, không thể tạo yêu cầu trả hàng.");


            string returnRequestCode = await _returnRepo.GenerateRequestReturnCodeAsync();

            var returnRequest = new ReturnRequest
            {
                OrderId = orderId,
                CreatedByUserId = userId,
                Status = "Pending",
                ReturnRequestCode = returnRequestCode,
                CreatedAt = GetVietnamTime(),
                Details = new List<ReturnRequestDetail>()
            };

            foreach (var (orderDetailId, quantity, reason) in itemDetails)
            {
                // Chuẩn hóa reason ngay tại đây
                var normalizedReason = NormalizeString(reason);

                var orderDetail = await _orderRepo.GetOrderDetailByIdAsync(orderDetailId);
                if (orderDetail == null) throw new Exception($"Không tìm thấy OrderDetail {orderDetailId}");

                long productId = orderDetail.ProductId;

                int totalReturned = await _returnRepo.GetTotalReturnedQuantityAsync(orderDetailId);
                int availableQuantity = orderDetail.Quantity - totalReturned;

                if (quantity > availableQuantity)
                    throw new Exception($"Số lượng trả vượt quá giới hạn cho OrderDetail {orderDetailId}. Có thể trả: {availableQuantity}");

                returnRequest.Details.Add(new ReturnRequestDetail
                {
                    OrderDetailId = orderDetailId,
                    ProductId = productId,
                    QuantityReturned = quantity,
                    Reason = normalizedReason
                });
            }

            var savedRequest = await _returnRepo.CreateAsync(returnRequest);


            // ✅ Upload ảnh cho toàn bộ ReturnRequest (không còn liên quan đến từng detail)
            if (images != null && images.Count > 0)
            {
                var imageModel = new ImageModel { Files = images };
                var uploadedImages = await _imageService.UploadReturnImagesAsync(imageModel, savedRequest.ReturnRequestId); // << change here
                //await _returnRepo.AddRangeAsync(uploadedImages);
            }

            return savedRequest;
        }*/

        public async Task<ReturnRequest> CreateReturnRequestWithImagesAsync(
    Guid orderId,
    List<(Guid orderDetailId, int quantity, string reason)> itemDetails,
    Guid userId,
    List<IFormFile> images)
        {
            var order = await _orderRepo.GetOrderByIdAsync(orderId);
            if (order == null)
                throw new Exception("Không tìm thấy đơn hàng.");

            if (!string.Equals(order.Status, "Exported", StringComparison.OrdinalIgnoreCase))
                throw new Exception("Chỉ được phép tạo yêu cầu trả hàng cho đơn hàng đã xuất.");

            var exportReceipt = await _warehouseExportRepo.GetExportSaleByOrderIdAsync(orderId);
            if (exportReceipt == null)
                throw new Exception("Không tìm thấy phiếu xuất kho bán tương ứng.");

            var exportDate = exportReceipt.ExportDate;
            var nowVN = GetVietnamTime();
            var daysDiff = (nowVN.Date - exportDate.Date).TotalDays;
            if (daysDiff > 30)
                throw new Exception("Đơn hàng đã được xuất quá 30 ngày, không thể tạo yêu cầu trả hàng.");

            var existingReturn = await _returnRepo.GetLatestReturnRequestByOrderIdAsync(orderId);

            ReturnRequest returnRequest;
            bool isNewRequest = false;

            if (existingReturn == null ||
                existingReturn.Status == "Approved" ||
                existingReturn.Status == "Completed" ||
                existingReturn.Status == "Rejected")
            {
                string returnRequestCode = await _returnRepo.GenerateRequestReturnCodeAsync();
                returnRequest = new ReturnRequest
                {
                    OrderId = orderId,
                    CreatedByUserId = userId,
                    Status = "Pending",
                    ReturnRequestCode = returnRequestCode,
                    CreatedAt = GetVietnamTime(),
                    Details = new List<ReturnRequestDetail>()
                };
                isNewRequest = true;
            }
            else
            {
                returnRequest = existingReturn;
                if (returnRequest.Details == null)
                    returnRequest.Details = new List<ReturnRequestDetail>();
            }

            foreach (var (orderDetailId, quantity, reason) in itemDetails)
            {
                var normalizedReason = NormalizeString(reason);
                var orderDetail = await _orderRepo.GetOrderDetailByIdAsync(orderDetailId);
                if (orderDetail == null)
                    throw new Exception($"Không tìm thấy OrderDetail {orderDetailId}");

                long productId = orderDetail.ProductId;
                int totalReturned = await _returnRepo.GetTotalReturnedQuantityAsync(orderDetailId);
                int availableQuantity = orderDetail.Quantity - totalReturned;

                if (quantity > availableQuantity)
                    throw new Exception($"Số lượng trả vượt quá giới hạn cho OrderDetail {orderDetailId}. Có thể trả: {availableQuantity}");

                // ✅ So sánh theo cả OrderDetailId và Reason đã chuẩn hoá
                var existingDetail = returnRequest.Details
                    .FirstOrDefault(d =>
                        d.OrderDetailId == orderDetailId &&
                        NormalizeString(d.Reason) == normalizedReason);

                if (existingDetail != null)
                {
                    // ✅ Nếu trùng OrderDetailId + Reason → cộng dồn
                    existingDetail.QuantityReturned += quantity;
                }
                else
                {
                    // ✅ Nếu khác Reason hoặc OrderDetailId mới → thêm mới
                    returnRequest.Details.Add(new ReturnRequestDetail
                    {
                        OrderDetailId = orderDetailId,
                        ProductId = productId,
                        QuantityReturned = quantity,
                        Reason = normalizedReason
                    });
                }
            }

            ReturnRequest savedRequest;
            if (isNewRequest)
            {
                savedRequest = await _returnRepo.CreateAsync(returnRequest);
            }
            else
            {
                savedRequest = await _returnRepo.UpdateReturnAsync(returnRequest);
            }

            if (images != null && images.Count > 0)
            {
                var imageModel = new ImageModel { Files = images };
                await _imageService.UploadReturnImagesAsync(imageModel, savedRequest.ReturnRequestId);
            }

            var managerUserId = order?.RequestProduct?.AgencyAccount?.ManagedByEmployee?.UserId;

            if (managerUserId != null && managerUserId != userId)
            {
                var agencyName = order.RequestProduct?.AgencyAccount?.AgencyName ?? "Đại lý";
                string message = $"📥 Đại lý {agencyName} vừa tạo yêu cầu trả hàng cho đơn {order.OrderCode}.";

                // Gửi thông báo qua SignalR
                await _hub.Clients.User(managerUserId.Value.ToString()).SendAsync("ReceiveNotification", new
                {
                    title = "ReturnSales",
                    message,
                    payload = savedRequest.ReturnRequestId
                });

                // Ghi thông báo vào DB
                var timeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
                var vietnamNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);

                var notification = new Notification
                {
                    UserId = managerUserId.Value,
                    Title = "Yêu cầu trả hàng mới",
                    Message = message,
                    Url = $"/sales/review-order",
                    CreatedAt = vietnamNow
                };

                await _notificationRepository.AddAsync(notification);
                await _notificationRepository.SaveChangesAsync();
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

            var order = await _orderRepo.GetOrderByIdAsync(request.OrderId);
            if (order == null)
                throw new Exception("Không tìm thấy đơn hàng.");

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

            var warehouseUserId = await _employeeRepo.GetUserIdByWarehouseIdAsync(warehouseId);
            if (warehouseUserId != null)
            {
                var agencyName = order.RequestProduct?.AgencyAccount?.AgencyName ?? "Đại lý";
                string message = $"📦 Có một đơn hàng trả về đã được duyệt từ đại lý {agencyName}. Vui lòng kiểm tra và xử lý.";

                // Gửi SignalR
                await _hub.Clients.User(warehouseUserId.ToString()).SendAsync("ReceiveNotification", new
                {
                    title = "ReturnKho",
                    message,
                    payload = receipt.ReturnWarehouseReceiptId
                });

                // Lưu thông báo vào DB
                var timeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
                var vietnamNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);

                var notification = new Notification
                {
                    UserId = warehouseUserId.Value,
                    Title = "Phiếu nhập trả hàng",
                    Message = message,
                    Url = $"/warehouse/view-export/",
                    CreatedAt = vietnamNow
                };

                await _notificationRepository.AddAsync(notification);
                await _notificationRepository.SaveChangesAsync();
            }
        }
       
            public async Task WarehouseRejectReturnRequestAsync(long ReturnWarehouseReceiptId, Guid userId, string rejectReason)
        {
            if (!await _employeeRepo.ExistsAsync(userId))
                throw new UnauthorizedAccessException("Bạn không phải Sale.");

            var requestWarehouse = await _returnRepo.GetReturnWarehouseReceiptWithDetailsAsync(ReturnWarehouseReceiptId);

            var requestReturn = await _returnRepo.GetByIdWithDetailsAsync(requestWarehouse.ReturnRequestId);
            if (requestWarehouse == null)
                throw new Exception("Không tìm thấy yêu cầu trả hàng.");

            if (!string.Equals(requestWarehouse.Status, "Pending", StringComparison.OrdinalIgnoreCase))
                throw new Exception("Yêu cầu đã được xử lý trước đó!");

            // 2. Lấy Order liên quan
            var order = await _orderRepository.GetOrderByIdAsync(requestReturn.OrderId)
                ?? throw new Exception("Không tìm thấy đơn đặt hàng liên quan.");

            // 3. Lấy RequestProduct liên quan
            var requestProduct = await _requestProductRepository.GetRequestProductByRequestIdAsync(order.RequestId)
                ?? throw new Exception("Không tìm thấy yêu cầu sản phẩm liên quan.");

            requestWarehouse.Status = "Rejected";
            requestWarehouse.Reason = rejectReason;
            requestReturn.Status = "Rejected"; // Cập nhật trạng thái yêu cầu trả hàng
            requestReturn.RejectedAt = DateTime.Now;
            requestReturn.RejectedBy = userId; // Ghi lại người từ chối

            await _returnRepo.UpdateAsync(requestReturn);
            await _returnRepo.UpdateReturnWarehouseAsync(requestWarehouse);
            await _returnRepo.SaveChangesAsync();

            var agencyId = requestProduct.AgencyId;
            // 3. Lấy AgencyAccount (hoặc bảng đại lý) từ AgencyId
            var agencyAccount = await _userRepository.GetAgencyAccountByIdAsync(agencyId)
                ?? throw new Exception("Không tìm thấy tài khoản đại lý.");

            var agencyUserId = agencyAccount.UserId; // Đổi tên biến
            var customerUser = await _userRepository.GetByIdAsync(agencyUserId)
                ?? throw new Exception("Không tìm thấy người dùng của đại lý.");
            // 6. Lấy email và tên
            var customerEmail = customerUser.Email;
            var customerName = agencyAccount.AgencyName; // hoặc user.FullName nếu có
            // ==== ĐẶT LỆNH GỬI EMAIL Ở ĐÂY ====
            await _emailService.SendReturnOrderCancelNotificationEmailAsync(
                customerEmail,
                customerName,
                requestReturn.ReturnRequestCode
            );
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

            // 2. Lấy Order liên quan
            var order = await _orderRepository.GetOrderByIdAsync(request.OrderId)
                ?? throw new Exception("Không tìm thấy đơn đặt hàng liên quan.");

            // 3. Lấy RequestProduct liên quan
            var requestProduct = await _requestProductRepository.GetRequestProductByRequestIdAsync(order.RequestId)
                ?? throw new Exception("Không tìm thấy yêu cầu sản phẩm liên quan.");

            request.Status = "Rejected";
            request.RejectedAt = DateTime.UtcNow;
            request.RejectedBy = userId;
            request.Reason = rejectReason;

            await _returnRepo.UpdateAsync(request);
            await _returnRepo.SaveChangesAsync();

            var agencyId = requestProduct.AgencyId;
            // 3. Lấy AgencyAccount (hoặc bảng đại lý) từ AgencyId
            var agencyAccount = await _userRepository.GetAgencyAccountByIdAsync(agencyId)
                ?? throw new Exception("Không tìm thấy tài khoản đại lý.");

            var agencyUserId = agencyAccount.UserId; // Đổi tên biến
            var customerUser = await _userRepository.GetByIdAsync(agencyUserId)
                ?? throw new Exception("Không tìm thấy người dùng của đại lý.");
            // 6. Lấy email và tên
            var customerEmail = customerUser.Email;
            var customerName = agencyAccount.AgencyName; // hoặc user.FullName nếu có
            // ==== ĐẶT LỆNH GỬI EMAIL Ở ĐÂY ====
            await _emailService.SendReturnOrderCancelNotificationEmailAsync(
                customerEmail,
                customerName,
                request.ReturnRequestCode
            );
        }


        public async Task<List<ReturnRequest>> GetPendingReturnsAsync()
        {
            return await _returnRepo.GetPendingApprovalAsync();
        }


        public async Task<List<ReturnRequestProdductDto>> GetAllReturnRequestsAsync()
        {
            var requests = await _returnRepo.GetAllAsync();

            return requests.OrderByDescending(r => r.CreatedAt).Select(r => new ReturnRequestProdductDto
            {
                ReturnRequestId = r.ReturnRequestId,
                OrderId = r.OrderId,
                CreatedAt = r.CreatedAt,
                CreatedByUserName = r.Order.RequestProduct.AgencyAccount.User.Username,
                ReturnRequestCode = r.ReturnRequestCode,
                Status = r.Status,
                Reason = r.Reason,
                Details = r.Details?.Select(d => new ReturnRequestProdductDetailDto
                {
                    ReturnRequestDetailId = d.ReturnRequestDetailId,
                    OrderDetailId = d.OrderDetailId,
                    ProductName = d.Product.ProductName,
                    Reason = d.Reason,
                    QuantityReturned = d.QuantityReturned,
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
                Reason = r.Reason,
                Details = r.Details?.Select(d => new ReturnRequestProdductDetailDto
                {
                    ReturnRequestDetailId = d.ReturnRequestDetailId,
                    OrderDetailId = d.OrderDetailId,
                    ProductName = d.Product.ProductName,
                    Reason = d.Reason,
                    QuantityReturned = d.QuantityReturned,
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
                CreatedByUserName = r.Order.RequestProduct.AgencyAccount.AgencyName,
                ReturnRequestCode = r.ReturnRequestCode,
                Status = r.Status,
                Reason = r.Reason,
                Details = r.Details?.Select(d => new ReturnRequestProdductDetailDto
                {
                    ReturnRequestDetailId = d.ReturnRequestDetailId,
                    OrderDetailId = d.OrderDetailId,
                    ProductName = d.Product?.ProductName ?? "",
                    Reason = d.Reason,
                    QuantityReturned = d.QuantityReturned,
                }).ToList() ?? new List<ReturnRequestProdductDetailDto>(),

                Images = r.Images?.Select(img => new ReturnRequestImageDto
                {
                    ReturnRequestImageId = img.ReturnRequestImageId,
                    ImageUrl = img.ImageUrl
                }).ToList() ?? new List<ReturnRequestImageDto>()
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
                CreatedByUserName = r.Order.RequestProduct.AgencyAccount.AgencyName,
                ReturnRequestCode = r.ReturnRequestCode,
                Status = r.Status,
                Reason = r.Reason,
                Details = r.Details?.Select(d => new ReturnRequestProdductDetailDto
                {
                    ReturnRequestDetailId = d.ReturnRequestDetailId,
                    OrderDetailId = d.OrderDetailId,
                    ProductName = d.Product?.ProductName ?? "",
                    Reason = d.Reason,
                    QuantityReturned = d.QuantityReturned,
                }).ToList() ?? new List<ReturnRequestProdductDetailDto>(),
                Images = r.Images?.Select(img => new ReturnRequestImageDto
                {
                    ReturnRequestImageId = img.ReturnRequestImageId,
                    ImageUrl = img.ImageUrl
                }).ToList() ?? new List<ReturnRequestImageDto>()
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
                CreatedByUserName = r.Order.RequestProduct.AgencyAccount.AgencyName,
                ReturnRequestCode = r.ReturnRequestCode,
                Status = r.Status,
                Reason = r.Reason,
                Details = r.Details?.Select(d => new ReturnRequestProdductDetailDto
                {
                    ReturnRequestDetailId = d.ReturnRequestDetailId,
                    OrderDetailId = d.OrderDetailId,
                    ProductName = d.Product.ProductName,
                    Reason = d.Reason,
                    QuantityReturned = d.QuantityReturned,
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
                Reason = r.Reason,
                Details = r.Details?.Select(d => new ReturnRequestProdductDetailDto
                {
                    ReturnRequestDetailId = d.ReturnRequestDetailId,
                    OrderDetailId = d.OrderDetailId,
                    ProductName = d.Product.ProductName,
                    Reason = d.Reason,
                    QuantityReturned = d.QuantityReturned,
                }).ToList()
            };
        }


        public async Task<List<ReturnWarehouseReceiptDto>> GetAllReturnWarehouseReceiptsAsync()
        {
            var receipts = await _warehouseReceiptRepo.GetAllAsync();

            return receipts.OrderByDescending(r => r.CreatedAt).Select(r => new ReturnWarehouseReceiptDto
            {
                ReturnWarehouseReceiptId = r.ReturnWarehouseReceiptId,
                ReceiptCode = r.ReceiptCode,
                ReceiptDate = r.ReceiptDate,
                CreatedAt = r.CreatedAt,
                CreatedByUserName = r.ReturnRequest.Order.RequestProduct.AgencyAccount.User.Username,
                ReturnRequestId = r.ReturnRequestId,
                ReturnRequestCode = r.ReturnRequest.ReturnRequestCode,
                WarehouseId = r.WarehouseId,
                reason = r.Reason,
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
                        BatchCode = d.Batch.BatchCode,
                        Reason = d.Reason
                    };
                }).ToList() ?? new List<ReturnWarehouseReceiptDetailDto>(),
                Images = r.ReturnRequest.Images?.Select(img => new ReturnRequestImageDto
                {
                    ReturnRequestImageId = img.ReturnRequestImageId,
                    ImageUrl = img.ImageUrl
                }).ToList() ?? new List<ReturnRequestImageDto>()
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
                reason = r.Reason,
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
                        BatchCode = d.Batch.BatchCode,
                        Reason = d.Reason,
                    };
                }).ToList() ?? new List<ReturnWarehouseReceiptDetailDto>(),
                Images = r.ReturnRequest.Images?.Select(img => new ReturnRequestImageDto
                {
                    ReturnRequestImageId = img.ReturnRequestImageId,
                    ImageUrl = img.ImageUrl
                }).ToList() ?? new List<ReturnRequestImageDto>() //them dong nay vao
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
                /*ReturnWarehouseReceiptId = r.ReturnWarehouseReceiptId,
                ReturnRequestId = r.ReturnRequestId,
                ReturnRequestCode = r.ReturnRequest?.ReturnRequestCode, // 🔥 dùng ? để tránh lỗi nếu ReturnRequest null
                ReceiptCode = r.ReceiptCode,
                ReceiptDate = r.ReceiptDate,
                WarehouseId = r.WarehouseId,
                reason = r.Reason,
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
                        BatchCode = d.Batch.BatchCode,
                        Reason = d.Reason,
                    };
                }).ToList() ?? new List<ReturnWarehouseReceiptDetailDto>()*/

                ReturnWarehouseReceiptId = r.ReturnWarehouseReceiptId,
                ReceiptCode = r.ReceiptCode,
                ReceiptDate = r.ReceiptDate,
                CreatedAt = r.CreatedAt,
                CreatedByUserName = r.ReturnRequest.Order.RequestProduct.AgencyAccount.User.Username,
                ReturnRequestId = r.ReturnRequestId,
                ReturnRequestCode = r.ReturnRequest.ReturnRequestCode,
                WarehouseId = r.WarehouseId,
                reason = r.Reason,
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
                        BatchCode = d.Batch.BatchCode,
                        Reason = d.Reason
                    };
                }).ToList() ?? new List<ReturnWarehouseReceiptDetailDto>(),
                Images = r.ReturnRequest.Images?.Select(img => new ReturnRequestImageDto
                {
                    ReturnRequestImageId = img.ReturnRequestImageId,
                    ImageUrl = img.ImageUrl
                }).ToList() ?? new List<ReturnRequestImageDto>()
            }).ToList();

            return result;
        }

        public async Task<IEnumerable<ReturnRequestProdductDto>> GetReturnRequestsByUserIdAsync(Guid userId)
        {
            var requests = await _returnRepo.GetByUserIdAsync(userId) ?? new List<ReturnRequest>(); ;
            var user = await _employeeRepo.GetByIdAsync(userId);
            var agencyName = user?.AgencyAccount?.AgencyName ?? "Unknown";

            return requests.Select(r => new ReturnRequestProdductDto
            {
                ReturnRequestId = r.ReturnRequestId,
                OrderId = r.OrderId,
                OrderCode = r.Order?.OrderCode ?? "Unknown",
                CreatedAt = r.CreatedAt,
                CreatedByUserName = agencyName,
                Status = r.Status,
                ReturnRequestCode = r.ReturnRequestCode,
                Reason = r.Reason,
                Details = r.Details?.Select(d => new ReturnRequestProdductDetailDto
                {
                    ReturnRequestDetailId = d.ReturnRequestDetailId,
                    OrderDetailId = d.OrderDetailId,
                    ProductName = d.Product?.ProductName ?? "",
                    Reason = d.Reason,
                    QuantityReturned = d.QuantityReturned,
                }).ToList() ?? new List<ReturnRequestProdductDetailDto>(),
                Images = r.Images?.Select(img => new ReturnRequestImageDto
                {
                    ReturnRequestImageId = img.ReturnRequestImageId,
                    ImageUrl = img.ImageUrl
                }).ToList() ?? new List<ReturnRequestImageDto>()
            }).ToList();
        }


        public async Task<ReturnRequestProdductDto> GetReturnRequestByIdAsync(Guid returnRequestId, Guid userId)
        {
            var request = await _returnRepo.GetByIdAndUserIdAsync(returnRequestId, userId);
            if (request == null) return null;

            var user = await _employeeRepo.GetByIdAsync(userId);
            var agencyName = user?.AgencyAccount?.AgencyName ?? "Unknown";

            return new ReturnRequestProdductDto
            {
                ReturnRequestId = request.ReturnRequestId,
                OrderId = request.OrderId,
                OrderCode = request.Order?.OrderCode ?? "Unknown",
                CreatedAt = request.CreatedAt,
                CreatedByUserName = agencyName,
                Status = request.Status,
                ReturnRequestCode = request.ReturnRequestCode,
                Reason = request.Reason,
                Details = request.Details?.Select(d => new ReturnRequestProdductDetailDto
                {
                    ReturnRequestDetailId = d.ReturnRequestDetailId,
                    OrderDetailId = d.OrderDetailId,
                    ProductName = d.Product?.ProductName ?? "",
                    Reason = d.Reason,
                    QuantityReturned = d.QuantityReturned,
                }).ToList() ?? new List<ReturnRequestProdductDetailDto>(),
                Images = request.Images?.Select(img => new ReturnRequestImageDto
                {
                    ReturnRequestImageId = img.ReturnRequestImageId,
                    ImageUrl = img.ImageUrl
                }).ToList() ?? new List<ReturnRequestImageDto>()
            };
        }

        public async Task<ReturnWarehouseReceiptDashboardDto> GetReturnWarehouseReceiptDashboardAsync(DateTime? fromDate, DateTime? toDate)
        {
            var vietnamNow = GetVietnamTime();
            var startDate = fromDate ?? new DateTime(vietnamNow.Year, vietnamNow.Month, 1);
            var endDate = toDate ?? vietnamNow.Date;

            // Lấy danh sách phiếu nhập trả hàng theo khoảng ngày
            var receipts = await _returnWarehouseReceiptRepo.GetByDateRangeAsync(startDate, endDate);

            // Tổng số lượng trả = tổng sum Quantity trong Details
            //them comment ở chỗ này để test
            var groupedByDate = receipts
                .GroupBy(r => r.ReceiptDate.Date)
                .Select(g => new DailyReturnWarehouseReceiptSummaryDto
                {
                    Date = g.Key,
                    Month = g.Key.Month,
                    Year = g.Key.Year,
                    TotalReturnReceipts = g.Count(),
                    TotalQuantity = g.Sum(r => r.Details?.Sum(d => d.Quantity) ?? 0),
                })
                .OrderBy(d => d.Date)
                .ToList();

            return new ReturnWarehouseReceiptDashboardDto
            {
                DailySummaries = groupedByDate,
                TotalReturnReceipts = receipts.Count,
                TotalQuantity = receipts.Sum(r => r.Details?.Sum(d => d.Quantity) ?? 0),
            };
        }


        public async Task<ReturnWarehouseReceiptDashboardDto> GetReturnWarehouseDashboardByUserWarehouseAsync(Guid userId, DateTime? fromDate, DateTime? toDate)
        {
            var warehouses = await _warehouseRepo.GetWarehousesByUserIdAsync(userId);
            var warehouseIds = warehouses.Select(w => w.WarehouseId).ToList();

            if (!warehouseIds.Any())
            {
                return new ReturnWarehouseReceiptDashboardDto
                {
                    TotalReturnReceipts = 0,
                    TotalQuantity = 0,
                    DailySummaries = new List<DailyReturnWarehouseReceiptSummaryDto>()
                };
            }

            var nowVN = GetVietnamTime();
            var from = fromDate ?? new DateTime(nowVN.Year, nowVN.Month, 1);
            var to = toDate ?? nowVN.Date;

            var receipts = await _returnWarehouseReceiptRepo.GetByWarehouseIdsAndDateRangeAsync(warehouseIds, from, to);

            var groupedByDate = receipts
                .GroupBy(r => r.ReceiptDate.Date)
                .Select(g => new DailyReturnWarehouseReceiptSummaryDto
                {
                    Date = g.Key,
                    Month = g.Key.Month,
                    Year = g.Key.Year,
                    TotalReturnReceipts = g.Count(),
                    TotalQuantity = g.Sum(r => r.Details?.Sum(d => d.Quantity) ?? 0)
                })
                .OrderBy(r => r.Date)
                .ToList();

            return new ReturnWarehouseReceiptDashboardDto
            {
                DailySummaries = groupedByDate,
                TotalReturnReceipts = receipts.Count,
                TotalQuantity = receipts.Sum(r => r.Details?.Sum(d => d.Quantity) ?? 0)
            };
        }



    }

}
