using BusinessObject.DTO.RequestExport;
using BusinessObject.Models;
using Repo.IRepository;
using Services.IService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Service
{
    public class RequestExportService : IRequestExportService
    {
        private readonly IExportRepository _requestExportRepository;
        private readonly ITemporaryWarehouseExportRepository _temporaryWarehouseRepository;

        public RequestExportService(IExportRepository requestExportRepository
            , ITemporaryWarehouseExportRepository temporaryWarehouseRepository)
        {
            _requestExportRepository = requestExportRepository;
            _temporaryWarehouseRepository = temporaryWarehouseRepository;
        }

        /*public async Task<List<RequestExportDto>> GetAllRequestExportsAsync(string? sortBy = null)
        {
            var requestExports = await _requestExportRepository.GetAllRequestExportsAsync();

            // ✅ Ánh xạ độ ưu tiên cho từng trạng thái
            var statusPriority = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
    {
        { "Pending", 0 },
        { "Requested", 1 },
        { "Approved", 2 }
    };

            // ✅ Sắp xếp theo yêu cầu
            if (!string.IsNullOrEmpty(sortBy))
            {
                switch (sortBy.ToLower())
                {
                    case "status":
                        requestExports = requestExports
                            .OrderBy(re => statusPriority.ContainsKey(re.Status) ? statusPriority[re.Status] : 99)
                            .ToList();
                        break;

                    case "approveddate_desc":
                        requestExports = requestExports
                            .OrderByDescending(re => re.ApprovedDate ?? DateTime.MinValue)
                            .ToList();
                        break;

                    case "approveddate_asc":
                        requestExports = requestExports
                            .OrderBy(re => re.ApprovedDate ?? DateTime.MinValue)
                            .ToList();
                        break;
                    case "requestdate_desc":
                        requestExports = requestExports
                            .OrderByDescending(re => re.RequestDate)
                            .ToList();
                        break;

                    case "requestdate_asc":
                        requestExports = requestExports
                            .OrderBy(re => re.RequestDate)
                            .ToList();
                        break;
                }
            }

            return requestExports.Select(re => new RequestExportDto
            {
                RequestExportId = re.RequestExportId,
                OrderId = re.OrderId,
                AgencyName = re.RequestedByAgency?.AgencyName ?? "Unknown",
                RequestDate = re.RequestDate,
                ApprovedByName = re.ApprovedByEmployee?.FullName ?? "Chưa duyệt",
                Status = re.Status,
                ApprovedDate = re.ApprovedDate,
                Note = re.Note,
                RequestExportCode = re.RequestExportCode,
                RequestExportDetails = re.RequestExportDetails.Select(red => new RequestExportDetailDto
                {
                    RequestExportDetailId = red.RequestItemId,
                    ProductId = red.ProductId,
                    ProductName = red.Product?.ProductName ?? "N/A",
                    Unit = red.Product?.Unit ?? "N/A",
                    Price = red.Product?.Price ?? 0,
                    RequestedQuantity = red.RequestedQuantity
                }).ToList()
            }).ToList();
        }*/

        public async Task<List<RequestExportDto>> GetAllRequestExportsAsync(string? sortBy = null)
        {
            var requestExports = await _requestExportRepository.GetAllRequestExportsAsync();

            // Lấy danh sách OrderId duy nhất từ RequestExport
            var orderIds = requestExports
                .Select(x => x.OrderId)
                .Distinct()
                .ToList();

            // Gọi repository để lấy toàn bộ TemporaryStockExport theo các OrderId
            var tempExports = await _temporaryWarehouseRepository.GetByOrderIdsAsync(orderIds);

            // Group theo OrderId để ánh xạ nhanh hơn
            var tempExportDict = tempExports
                .GroupBy(t => t.OrderId)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Sắp xếp nếu cần
            var statusPriority = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
                {
                    { "Pending", 0 },
                    { "Requested", 1 },
                    { "Approved", 2 }
                };

            if (!string.IsNullOrEmpty(sortBy))
            {
                switch (sortBy.ToLower())
                {
                    case "status":
                        requestExports = requestExports
                            .OrderBy(re => statusPriority.ContainsKey(re.Status) ? statusPriority[re.Status] : 99)
                            .ToList();
                        break;
                    case "approveddate_desc":
                        requestExports = requestExports
                            .OrderByDescending(re => re.ApprovedDate ?? DateTime.MinValue)
                            .ToList();
                        break;
                    case "approveddate_asc":
                        requestExports = requestExports
                            .OrderBy(re => re.ApprovedDate ?? DateTime.MinValue)
                            .ToList();
                        break;
                    case "requestdate_desc":
                        requestExports = requestExports
                            .OrderByDescending(re => re.RequestDate)
                            .ToList();
                        break;
                    case "requestdate_asc":
                        requestExports = requestExports
                            .OrderBy(re => re.RequestDate)
                            .ToList();
                        break;
                }
            }

            // Ánh xạ sang DTO
            return requestExports.Select(re => new RequestExportDto
            {
                RequestExportId = re.RequestExportId,
                OrderId = re.OrderId,
                AgencyName = re.RequestedByAgency?.AgencyName ?? "Unknown",
                RequestDate = re.RequestDate,
                ApprovedByName = re.ApprovedByEmployee?.FullName ?? "Chưa duyệt",
                Status = re.Status,
                ApprovedDate = re.ApprovedDate,
                Note = re.Note,
                RequestExportCode = re.RequestExportCode,

                RequestExportDetails = re.RequestExportDetails.Select(red => new RequestExportDetailDto
                {
                    RequestExportDetailId = red.RequestItemId,
                    ProductId = red.ProductId,
                    ProductName = red.Product?.ProductName ?? "N/A",
                    Unit = red.Product?.Unit ?? "N/A",
                    Price = red.Product?.Price ?? 0,
                    RequestedQuantity = red.RequestedQuantity
                }).ToList(),

                TemporaryStockExportDetails = tempExportDict.ContainsKey(re.OrderId)
                    ? tempExportDict[re.OrderId].Select(tse => new TemporaryStockExportDto
                        {
                            WarehouseId = tse.WarehouseId,
                            ProductId = tse.ProductId,
                            BatchId = tse.BatchId,
                            Quantity = tse.Quantity
                        }).ToList()
    :                   new List<TemporaryStockExportDto>()

            }).ToList();
        }


        public async Task<RequestExportDto> GetRequestExportByIdAsync(int requestId)
        {
            var requestExport = await _requestExportRepository.GetRequestExportById(requestId);

            if (requestExport == null)
            {
                return null; // hoặc throw exception nếu cần
            }

            return new RequestExportDto
            {
                RequestExportId = requestExport.RequestExportId,
                OrderId = requestExport.OrderId,
                AgencyName = requestExport.RequestedByAgency?.AgencyName ?? "Unknown", // 👈 Gán tên đại lý
                RequestDate = requestExport.RequestDate,
                ApprovedByName = requestExport.ApprovedByEmployee?.FullName ?? "Chưa duyệt",
                Status = requestExport.Status,
                ApprovedDate = requestExport.ApprovedDate,
                Note = requestExport.Note,
                RequestExportCode = requestExport.RequestExportCode,
                RequestExportDetails = requestExport.RequestExportDetails != null
                    ? requestExport.RequestExportDetails.Select(red => new RequestExportDetailDto
                    {
                        RequestExportDetailId = red.RequestItemId,
                        ProductId = red.ProductId,
                        ProductName = red.Product?.ProductName ?? "N/A",
                        Unit = red.Product?.Unit ?? "N/A",
                        Price = red.Product?.Price ?? 0, // hoặc giá khác nếu có
                        RequestedQuantity = red.RequestedQuantity
                    }).ToList()
                    : new List<RequestExportDetailDto>() // Trả về list rỗng nếu null
            };
        }


    }
}
