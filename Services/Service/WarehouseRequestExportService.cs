using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessObject.DTO.Warehouse;
using BusinessObject.Models;
using DataAccessLayer;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Repo.IRepository;
using Services.IService;

namespace Services.Service
{
    public class WarehouseRequestExportService : IWarehouseRequestExportService
    {
        private readonly IWarehouseRequestExportRepository _repository;
        private readonly IExportRepository _requestExportRepository;
        private readonly ITemporaryWarehouseExportRepository _tempExportRepo;
        private readonly MinhLongDbContext _context;
        private readonly IHubContext<NotificationHub> _hub;

        public WarehouseRequestExportService(IWarehouseRequestExportRepository repository
            , MinhLongDbContext context
            , IHubContext<NotificationHub> hub
            , IExportRepository requestExportRepository
            , ITemporaryWarehouseExportRepository tempExportRepo)
        {
            _repository = repository;
            _context = context;
            _hub = hub;
            _requestExportRepository = requestExportRepository;
            _tempExportRepo = tempExportRepo;
        }

        /* public async Task<WarehouseRequestExport> CreateWarehouseRequestExportAsync(long warehouseId, int requestExportId)
         {
             var result = await _repository.CreateWarehouseRequestExportAsync(warehouseId, requestExportId);

             *//*// ✅ Gửi thông báo cho KHO (GroupId = 3)
             await _hub.Clients.Group("3").SendAsync("ReceiveNotification",
                 $"🚚 Yêu cầu xuất kho mới!");*//*

             // ✅ Gửi thông báo cho KHO (GroupId = 3)
             var notification = new
             {
                 title = "Kho", // Tiêu đề thông báo
                 message = $"🚚 Yêu cầu xuất kho mới!", // Nội dung thông báo
                 payload = "Yêu cầu xuất kho", // Bạn có thể thay bằng thông tin chi tiết nếu muốn
             };

             // Gửi thông báo qua SignalR
             await _hub.Clients.Group("3")
                 .SendAsync("ReceiveNotification", notification);


             return result;
         }*/

        /// <summary>
        /// Tạo WarehouseRequestExport dựa trên RequestExport + bản ghi tạm (TemporaryStockExport).
        /// </summary>
        /// <param name="warehouseId">Kho đích do Sale gán</param>
        /// <param name="requestExportId">Id của RequestExport</param>
        public async Task<List<WarehouseRequestExport>> CreateWarehouseRequestExportAsync(long warehouseId, int requestExportId)
        {
            // 1) Lấy RequestExport
            var requestExport = await _requestExportRepository.GetRequestExportByIdAsync(requestExportId);
            if (requestExport == null || requestExport.RequestExportDetails == null || !requestExport.RequestExportDetails.Any())
            {
                return null; // hoặc throw new Exception tùy ý
            }

            // 2) Kiểm tra trạng thái
            if (requestExport.Status == "Requested" || requestExport.Status == "Approved")
            {
                throw new InvalidOperationException("This request has already been assigned to a warehouse.");
            }

            // 3) Lấy OrderId (nếu RequestExport chứa OrderId)
            //    Hoặc bạn lấy từ requestExport.Order (nếu đã Include)...
            var orderId = requestExport.OrderId;
            if (orderId == null)
            {
                throw new InvalidOperationException("RequestExport doesn't have associated OrderId.");
            }

            // 4) Lấy danh sách bản ghi tạm (TemporaryStockExport) theo OrderId
            //    Từ đó bạn sẽ lấy WarehouseId, ProductId, Quantity...
            var tempStockExports = await _tempExportRepo.GetByOrderIdAsync(orderId);
            if (tempStockExports == null || !tempStockExports.Any())
            {
                throw new InvalidOperationException("No temporary stock exports found for this order.");
            }

            // 5) Tạo danh sách WarehouseRequestExport dựa trên các bản ghi tạm
            //    => Kho có thể là warehouseId từ tham số (nếu muốn gán cho 1 kho đích),
            //       Hoặc lấy WarehouseId từ chính `tempStockExports` nếu bạn muốn
            var warehouseRequestExports = new List<WarehouseRequestExport>();
            foreach (var tmp in tempStockExports)
            {
                var wre = new WarehouseRequestExport
                {
                    WarehouseId = tmp.WarehouseId,       // => Nếu bạn muốn lấy kho từ TSE
                                                         //WarehouseId = warehouseId,         // => Nếu bạn muốn ép tất cả về kho do Sale chọn
                    RequestExportId = requestExportId,
                    ProductId = tmp.ProductId,
                    QuantityRequested = (int)tmp.Quantity,
                    RemainingQuantity = (int)tmp.Quantity,
                    Status = "PENDING"
                };
                warehouseRequestExports.Add(wre);
            }

            // 6) Lưu danh sách WarehouseRequestExport vào DB qua repository
            await _repository.AddRangeAsync(warehouseRequestExports);

            // 7) Cập nhật trạng thái RequestExport => "Requested"
            requestExport.Status = "Requested";
            await _requestExportRepository.UpdateRequestExportAsync(requestExport);

            // Lưu lại thay đổi
            await _requestExportRepository.SaveChangesAsync();

            // 8) Gửi thông báo SignalR cho nhóm "3"
            var notification = new
            {
                title = "Kho",
                message = "🚚 Yêu cầu xuất kho mới!",
                payload = $"RequestExportCode: {requestExport.RequestExportCode}"
            };
            await _hub.Clients.Group("3").SendAsync("ReceiveNotification", notification);

            // 9) Trả về danh sách WarehouseRequestExport vừa tạo
            return warehouseRequestExports;
        }


        public async Task<List<WarehouseRequestExportDtoResponse>> GetByWarehouseIdAsync(long warehouseId, string? sortBy = null)
        {
            var data = await _repository.GetByWarehouseIdAsync(warehouseId);

            // ✅ Sắp xếp theo yêu cầu
            if (!string.IsNullOrEmpty(sortBy))
            {
                switch (sortBy.ToLower())
                {
                    case "status":
                        // Pending trước, Approved sau
                        data = data.OrderBy(x => x.Status == "APPROVED" ? 1 : 0).ToList();
                        break;

                    case "createddate_desc":
                        data = data.OrderByDescending(x => x.RequestExport?.RequestDate ?? DateTime.MinValue).ToList();
                        break;

                    case "createddate_asc":
                        data = data.OrderBy(x => x.RequestExport?.RequestDate ?? DateTime.MinValue).ToList();
                        break;
                }
            }

            return data.Select(x => new WarehouseRequestExportDtoResponse
            {
                WarehouseRequestExportId = x.WarehouseRequestExportId,
                RequestExportId = x.RequestExportId,
                ProductId = x.ProductId,
                ProductName = x.Product?.ProductName,
                AgencyName = x.RequestExport?.Order?.RequestProduct?.AgencyAccount?.AgencyName,
                OrderCode = x.RequestExport?.Order?.OrderCode,
                QuantityRequested = x.QuantityRequested,
                RemainingQuantity = x.RemainingQuantity,
                Status = x.Status,
                ApprovedByFullName = x.User?.Employee?.FullName
            }).ToList();
        }

        public async Task<WarehouseRequestExportDtoResponse> GetByWarehouseRequestExportIdAsync(int warehouseRequestExportId)
        {
            var x = await _repository.GetByIdAsync(warehouseRequestExportId);

            if (x == null)
                throw new KeyNotFoundException("Warehouse request not found");

            return new WarehouseRequestExportDtoResponse
            {
                WarehouseRequestExportId = x.WarehouseRequestExportId,
                RequestExportId = x.RequestExportId,
                ProductId = x.ProductId,
                ProductName = x.Product?.ProductName,
                AgencyName = x.RequestExport?.Order?.RequestProduct?.AgencyAccount?.AgencyName,
                OrderCode = x.RequestExport?.Order?.OrderCode,
                QuantityRequested = x.QuantityRequested,
                RemainingQuantity = x.RemainingQuantity,
                Status = x.Status,
                ApprovedByFullName = x.User?.Employee?.FullName
            };
        }


        public async Task<bool> ApproveRequestAsync(int warehouseRequestExportId, int quantityApproved, Guid approvedBy)
        {
            var request = await _repository.GetByIdAsync(warehouseRequestExportId);
            if (request == null)
                throw new KeyNotFoundException("Warehouse request not found");

            if (request.QuantityRequested < quantityApproved)
                throw new InvalidOperationException("Approved quantity cannot exceed requested quantity");

            request.QuantityApproved = quantityApproved;
            request.ApprovedBy = approvedBy;
            request.Status = "APPROVED";
            request.RemainingQuantity = request.QuantityRequested - quantityApproved;

            await _repository.UpdateAsync(request);

            var relatedRequests = await _context.WarehouseRequestExports
        .Where(x => x.RequestExportId == request.RequestExportId)
        .ToListAsync();

            bool allCompleted = relatedRequests.All(x => x.RemainingQuantity == 0);

            if (allCompleted)
            {
                var requestExport = await _context.RequestExports
                    .FirstOrDefaultAsync(x => x.RequestExportId == request.RequestExportId);

                if (requestExport != null)
                {
                    requestExport.Status = "Approved";
                    _context.RequestExports.Update(requestExport);
                    await _context.SaveChangesAsync();
                }
            }

            return true;
        }

        public async Task<bool> ApproveAllRequestsByWarehouseAsync(int warehouseId, int requestExportId, Dictionary<int, int> quantitiesApproved, Guid approvedBy)
        {
            var requests = await _repository.GetByWarehouseAndRequestExportAsync(warehouseId, requestExportId);
            if (requests == null || !requests.Any())
                throw new KeyNotFoundException("No warehouse requests found for this WarehouseId and RequestExportId");

            foreach (var request in requests)
            {
                if (!quantitiesApproved.ContainsKey(request.WarehouseRequestExportId))
                    continue; // Nếu không có số lượng duyệt, bỏ qua

                int approvedQuantity = quantitiesApproved[request.WarehouseRequestExportId];

                if (approvedQuantity > request.QuantityRequested)
                    throw new InvalidOperationException($"Approved quantity for product {request.ProductId} exceeds requested quantity");

                request.QuantityApproved = approvedQuantity;
                request.ApprovedBy = approvedBy;
                request.Status = "APPROVED";
                request.RemainingQuantity = request.QuantityRequested - approvedQuantity;
            }

            await _repository.UpdateManyAsync(requests);
            return true;
        }

        
    }
}
