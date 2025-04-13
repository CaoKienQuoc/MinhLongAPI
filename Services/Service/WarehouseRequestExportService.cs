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
        private readonly IWarehouseTransferRepository _transferRepo;
        private readonly MinhLongDbContext _context;
        private readonly IHubContext<NotificationHub> _hub;

        public WarehouseRequestExportService(IWarehouseRequestExportRepository repository
            , MinhLongDbContext context
            , IHubContext<NotificationHub> hub
            , IExportRepository requestExportRepository
            , ITemporaryWarehouseExportRepository tempExportRepo
            ,IWarehouseTransferRepository transferRepo)
        {
            _repository = repository;
            _context = context;
            _hub = hub;
            _requestExportRepository = requestExportRepository;
            _tempExportRepo = tempExportRepo;
            _transferRepo = transferRepo;
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


        public async Task<List<WarehouseRequestExport>> CreateWarehouseRequestExportAsync(int requestExportId, Guid currentUserId)
        {
            var requestExport = await _requestExportRepository.GetRequestExportByIdAsync(requestExportId);
            if (requestExport == null || requestExport.RequestExportDetails == null || !requestExport.RequestExportDetails.Any())
                throw new InvalidOperationException("RequestExport not found or invalid.");

            if (requestExport.Status == "Requested" || requestExport.Status == "Approved")
                throw new InvalidOperationException("This request has already been assigned.");

            var orderId = requestExport.OrderId;
            if (orderId == Guid.Empty)
                throw new InvalidOperationException("OrderId is missing.");

            var tempStockExports = await _tempExportRepo.GetByOrderIdAsync(orderId);
            if (tempStockExports == null || !tempStockExports.Any())
                throw new InvalidOperationException("No temporary stock exports found.");

            var groupedByProduct = tempStockExports
                .GroupBy(t => t.ProductId)
                .ToDictionary(
                    g => g.Key,
                    g => g.GroupBy(x => x.WarehouseId)
                          .ToDictionary(y => y.Key, y => y.Sum(z => z.Quantity))
                );

            var warehouseRequestExports = new List<WarehouseRequestExport>();
            var warehouseTransfers = new List<WarehouseTransferRequest>();

            foreach (var productGroup in groupedByProduct)
            {
                var productId = productGroup.Key;
                var warehouseQuantities = productGroup.Value;

                var mainWarehouse = warehouseQuantities
                    .OrderByDescending(x => x.Value)
                    .First().Key;

                var mainQuantity = warehouseQuantities[mainWarehouse];

                warehouseRequestExports.Add(new WarehouseRequestExport
                {
                    WarehouseId = mainWarehouse,
                    RequestExportId = requestExportId,
                    ProductId = productId,
                    QuantityRequested = (int)mainQuantity,
                    RemainingQuantity = (int)mainQuantity,
                    Status = "Pending",
                    RequestedBy = currentUserId,
                    RequestedByAgencyId = requestExport.RequestedByAgencyId
                });

                foreach (var kvp in warehouseQuantities.Where(x => x.Key != mainWarehouse))
                {
                    var sourceWarehouseId = kvp.Key;
                    var quantity = kvp.Value;

                    var transfer = new WarehouseTransferRequest
                    {
                        SourceWarehouseId = sourceWarehouseId,
                        DestinationWarehouseId = mainWarehouse,
                        RequestedBy = currentUserId,
                        RequestDate = DateTime.UtcNow,
                        RequestExportId = requestExportId,
                        Status = "Pending",
                        Notes = $"Transfer ProductId {productId} - Qty: {quantity}",
                        TransferProducts = new List<WarehouseTransferProduct>
                {
                    new WarehouseTransferProduct
                    {
                        ProductId = productId,
                        Quantity = (int)quantity,
                        BatchId = tempStockExports
                            .FirstOrDefault(t => t.ProductId == productId && t.WarehouseId == sourceWarehouseId)?.BatchId
                    }
                }
                    };

                    warehouseTransfers.Add(transfer);
                }
            }

            await _repository.AddRangeAsync(warehouseRequestExports);
            await _transferRepo.AddRangeAsync(warehouseTransfers);

            requestExport.Status = "Requested";
            await _requestExportRepository.UpdateRequestExportAsync(requestExport);
            await _requestExportRepository.SaveChangesAsync();

            await _hub.Clients.Group("3").SendAsync("ReceiveNotification", new
            {
                title = "Kho",
                message = "🚚 Yêu cầu xuất kho mới!",
                payload = $"RequestExportCode: {requestExport.RequestExportCode}"
            });

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
                /*ApprovedByFullName = x.User?.Employee?.FullName*/
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
                //ApprovedByFullName = x.User?.Employee?.FullName
            };
        }


        public async Task<bool> ApproveRequestAsync(int warehouseRequestExportId, int quantityApproved, Guid approvedBy)
        {
            var request = await _repository.GetByIdAsync(warehouseRequestExportId);
            if (request == null)
                throw new KeyNotFoundException("Warehouse request not found");

            if (request.QuantityRequested < quantityApproved)
                throw new InvalidOperationException("Approved quantity cannot exceed requested quantity");

            request.RequestedBy = approvedBy;
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

                request.RequestedBy = approvedBy;
                request.Status = "APPROVED";
                request.RemainingQuantity = request.QuantityRequested - approvedQuantity;
            }

            await _repository.UpdateManyAsync(requests);
            return true;
        }


    }
}
