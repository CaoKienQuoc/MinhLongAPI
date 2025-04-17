using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessObject.DTO.Warehouse;
using BusinessObject.Models;
using Microsoft.AspNetCore.SignalR;
using Repo.IRepository;
using Services.IService;

namespace Services.Service
{
    public class WarehouseTransferService : IWarehouseTransferService
    {
        private readonly IWarehouseTransferRepository _repository;
        private readonly IHubContext<NotificationHub> _hub;

        public WarehouseTransferService(IWarehouseTransferRepository repository, IHubContext<NotificationHub> hub)
        {
            _repository = repository;
            _hub = hub;
        }

        public async Task<WarehouseTransferRequestDetailDto> CreateTransferRequestAsync(WarehouseTransferRequestCreateDto dto, Guid requestedBy)
        {
            // ✳️ Gọi repo thay vì DbContext trực tiếp
            var requestExport = await _repository.GetRequestExportWithOrderAsync(dto.RequestExportId);
            if (requestExport == null)
                throw new Exception("RequestExport không tồn tại.");

            var orderCode = requestExport.Order?.OrderCode;

            var request = new WarehouseTransferRequest
            {
                //RequestCode = $"REQ-{DateTime.UtcNow.Ticks}",
                //SourceWarehouseId = ourceWarehouseId ?? defaultWarehouseId,
                DestinationWarehouseId = dto.DestinationWarehouseId,
                //ExpectedDeliveryDate = dto.ExpectedDeliveryDate,
                //RequestedBy = requestedBy,
                RequestDate = DateTime.UtcNow,
                Status = "Pending",
                Notes = dto.Notes,
                //RequestExportId = dto.RequestExportId,
                //OrderCode = orderCode,
                TransferProducts = dto.Products.Select(p => new WarehouseTransferProduct
                {
                    ProductId = p.ProductId,
                    Quantity = p.Quantity,
                    //Unit = p.Unit,
                    //Notes = p.Notes
                }).ToList()
            };

            var created = await _repository.CreateAsync(request);

            // ✅ Gửi thông báo cho KHO (GroupId = 6)
            var notification = new
            {
                title = "Kho Tổng", // Tiêu đề thông báo
                message = $"🚚 Yêu cầu điều phối xuất kho mới!", // Nội dung thông báo
                //payload = created.RequestCode // hoặc thêm thông tin khác nếu cần
            };

            await _hub.Clients.Group("6")
                .SendAsync("ReceiveNotification", notification);
            return new WarehouseTransferRequestDetailDto
            {
                Id = created.Id,
                //RequestCode = created.RequestCode,
                SourceWarehouseId = created.SourceWarehouseId,
                DestinationWarehouseId = created.DestinationWarehouseId,
                RequestDate = created.RequestDate,
                Status = created.Status,
                Notes = created.Notes,
                //OrderCode = created.OrderCode,
                Products = created.TransferProducts.Select(tp => new WarehouseTransferProductDto
                {
                    ProductId = tp.ProductId,
                    Quantity = tp.Quantity,
                    //Unit = tp.Unit,
                    //Notes = tp.Notes
                }).ToList()
            };
        }
        public async Task<List<WarehouseTransferRequestDetailDto>> GetAllAsync()
        {
            var list = await _repository.GetAllAsync();

            return list.Select(r => new WarehouseTransferRequestDetailDto
            {
                Id = r.Id,
                //RequestCode = r.RequestCode,
                SourceWarehouseId = r.SourceWarehouseId,
                DestinationWarehouseId = r.DestinationWarehouseId,
                RequestDate = r.RequestDate,
                Status = r.Status,
                Notes = r.Notes,
                //OrderCode = r.OrderCode,
                Products = r.TransferProducts.Select(p => new WarehouseTransferProductDto
                {
                    ProductId = p.ProductId,
                    Quantity = p.Quantity,
                    //Unit = p.Unit,
                    //Notes = p.Notes
                }).ToList()
            }).ToList();
        }

        public async Task<WarehouseTransferRequestDetailDto?> GetByIdAsync(long id)
        {
            var r = await _repository.GetByIdAsync(id);
            if (r == null) return null;

            return new WarehouseTransferRequestDetailDto
            {
                Id = r.Id,
                //RequestCode = r.RequestCode,
                SourceWarehouseId = r.SourceWarehouseId,
                DestinationWarehouseId = r.DestinationWarehouseId,
                RequestDate = r.RequestDate,
                Status = r.Status,
                Notes = r.Notes,
                //OrderCode = r.OrderCode,
                Products = r.TransferProducts.Select(p => new WarehouseTransferProductDto
                {
                    ProductId = p.ProductId,
                    Quantity = p.Quantity,
                    //Unit = p.Unit,
                    //Notes = p.Notes
                }).ToList()
            };
        }

        public async Task<bool> PlanTransferRequestAsync(long requestId, long sourceWarehouseId, Guid plannerId)
        {
            return await _repository.PlanTransferRequestAsync(requestId, sourceWarehouseId, plannerId);
        }

        public async Task<List<WarehouseTransferRequestDetailDto>> GetRequestsToExportAsync(long sourceWarehouseId)
        {
            var list = await _repository.GetPlannedRequestsByWarehouseAsync(sourceWarehouseId);

            return list.Select(r => new WarehouseTransferRequestDetailDto
            {
                Id = r.Id,
                //RequestCode = r.RequestCode,
                SourceWarehouseId = r.SourceWarehouseId,
                DestinationWarehouseId = r.DestinationWarehouseId,
                RequestDate = r.RequestDate,
                Status = r.Status,
                Notes = r.Notes,
                //OrderCode = r.OrderCode,
                Products = r.TransferProducts.Select(p => new WarehouseTransferProductDto
                {
                    ProductId = p.ProductId,
                    Quantity = p.Quantity,
                   //Unit = p.Unit,
                    //Notes = p.Notes
                }).ToList()
            }).ToList();
        }

        public async Task<WarehouseTransferRequestDetailDto> AutoCreateTransferRequestFromRemainingAsync(AutoCreateTransferRequestDto dto, Guid requestedBy)
        {
            var requestExport = await _repository.GetRequestExportWithOrderAsync(dto.RequestExportId);
            if (requestExport == null)
                throw new Exception("RequestExport không tồn tại.");

            /*var remainingItems = await _repository.GetRemainingRequestExportsAsync(dto.RequestExportId);
            if (remainingItems == null || remainingItems.Count == 0)
                throw new Exception("Không có sản phẩm còn thiếu.");*/

            var transferRequest = new WarehouseTransferRequest
            {
                //RequestCode = $"REQ-{DateTime.UtcNow.Ticks}",
                DestinationWarehouseId = dto.DestinationWarehouseId,
                //ExpectedDeliveryDate = dto.ExpectedDeliveryDate,
                //RequestedBy = requestedBy,
                RequestDate = DateTime.UtcNow,
                Status = "Pending",
                Notes = dto.Notes,
                //RequestExportId = dto.RequestExportId,
                //OrderCode = requestExport.Order?.OrderCode,
               /* TransferProducts = remainingItems.Select(x => new WarehouseTransferProduct
                {
                    ProductId = x.ProductId,
                    Quantity = x.RemainingQuantity,
                    //Unit = x.Product.Unit, // có thể cải tiến sau
                    //Notes = "Auto from remaining quantity"
                }).ToList()*/
            };

            var created = await _repository.CreateAsync(transferRequest);

            // ✅ Gửi thông báo cho KHO (GroupId = 6)
            var notification = new
            {
                title = "Kho Tổng", // Tiêu đề thông báo
                message = $"🚚 Yêu cầu điều phối xuất kho mới!", // Nội dung thông báo
                //payload = created.RequestCode // hoặc thêm thông tin khác nếu cần
            };

            await _hub.Clients.Group("6")
                .SendAsync("ReceiveNotification", notification);

            return new WarehouseTransferRequestDetailDto
            {
                Id = created.Id,
                //RequestCode = created.RequestCode,
                SourceWarehouseId = created.SourceWarehouseId,
                DestinationWarehouseId = created.DestinationWarehouseId,
                RequestDate = created.RequestDate,
                Status = created.Status,
                Notes = created.Notes,
                //OrderCode = created.OrderCode,
                Products = created.TransferProducts.Select(tp => new WarehouseTransferProductDto
                {
                    ProductId = tp.ProductId,
                    Quantity = tp.Quantity,
                    //Unit = tp.Unit,
                   //Notes = tp.Notes
                }).ToList()
            };
        }

        public async Task<List<WarehouseTransferRequestDetailDto>> GetBySourceWarehouseAsync(long sourceWarehouseId)
        {
            var list = await _repository.GetBySourceWarehouseAsync(sourceWarehouseId);
            return list.Select(MapToDto).ToList();
        }

        public async Task<List<WarehouseTransferRequestDetailDto>> GetByDestinationWarehouseAsync(long destinationWarehouseId)
        {
            var list = await _repository.GetByDestinationWarehouseAsync(destinationWarehouseId);
            return list.Select(MapToDto).ToList();
        }

        private WarehouseTransferRequestDetailDto MapToDto(WarehouseTransferRequest r)
        {
            return new WarehouseTransferRequestDetailDto
            {
                Id = r.Id,
                //RequestCode = r.RequestCode,
                SourceWarehouseId = r.SourceWarehouseId,
                SourceWarehouseName = r.SourceWarehouse?.WarehouseName, 
                DestinationWarehouseId = r.DestinationWarehouseId,
                DestinationWarehouseName = r.DestinationWarehouse?.WarehouseName, 
                RequestDate = r.RequestDate,
                //OrderCode = r.OrderCode,
                Status = r.Status,
                Notes = r.Notes,
                Products = r.TransferProducts.Select(p => new WarehouseTransferProductDto
                {
                    ProductId = p.ProductId,
                    Quantity = p.Quantity,
                    //Unit = p.Unit,
                    //Notes = p.Notes
                }).ToList()
            };
        }




    }

}
