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
        private readonly IRequestExportRepository _requestExportRepository;
        private readonly ITemporaryWarehouseExportRepository _temporaryWarehouseRepository;

        public RequestExportService(IRequestExportRepository requestExportRepository
            , ITemporaryWarehouseExportRepository temporaryWarehouseRepository)
        {
            _requestExportRepository = requestExportRepository;
            _temporaryWarehouseRepository = temporaryWarehouseRepository;
        }

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
                Status = re.Status,
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
                return null;
            }

            // ✅ Lấy dữ liệu TemporaryStockExport theo OrderId
            var tempExports = await _temporaryWarehouseRepository.GetByOrderIdAsync(requestExport.OrderId);

            return new RequestExportDto
            {
                RequestExportId = requestExport.RequestExportId,
                OrderId = requestExport.OrderId,
                AgencyName = requestExport.RequestedByAgency?.AgencyName ?? "Unknown",
                RequestDate = requestExport.RequestDate,
                Status = requestExport.Status,
                Note = requestExport.Note,
                WarehouseId = requestExport.Order?.TemporaryStockExports?.FirstOrDefault()?.WarehouseId ?? 0,
                WarehouseName = requestExport.Order?.TemporaryStockExports?.FirstOrDefault()?.Warehouse?.WarehouseName ?? "Unknown",
                RequestExportCode = requestExport.RequestExportCode,

                RequestExportDetails = requestExport.RequestExportDetails != null
                    ? requestExport.RequestExportDetails.Select(red => new RequestExportDetailDto
                    {
                        RequestExportDetailId = red.RequestItemId,
                        ProductId = red.ProductId,
                        ProductName = red.Product?.ProductName ?? "N/A",
                        Unit = red.Product?.Unit ?? "N/A",
                        Price = red.Product?.Price ?? 0,
                        RequestedQuantity = red.RequestedQuantity
                    }).ToList()
                    : new List<RequestExportDetailDto>(),

                TemporaryStockExportDetails = tempExports != null && tempExports.Any()
                    ? tempExports.Select(tse => new TemporaryStockExportDto
                    {
                        WarehouseId = tse.WarehouseId,
                        ProductId = tse.ProductId,
                        BatchId = tse.BatchId,
                        Quantity = tse.Quantity
                    }).ToList()
                    : new List<TemporaryStockExportDto>()
            };
        }


        public async Task<List<RequestExportDto>> GetRequestExportsBySalesAsync(Guid salesUserId, string? sortBy = null)
        {
            var requestExports = await _requestExportRepository.GetRequestExportsBySalesUserIdAsync(salesUserId);

            var orderIds = requestExports.Select(x => x.OrderId).Distinct().ToList();
            var tempExports = await _temporaryWarehouseRepository.GetByOrderIdsAsync(orderIds);
            var tempExportDict = tempExports.GroupBy(t => t.OrderId).ToDictionary(g => g.Key, g => g.ToList());

            // Sắp xếp theo yêu cầu
            var statusPriority = new Dictionary<string, int> {
        { "Pending", 0 }, { "Requested", 1 }, { "Approved", 2 }
    };

            if (!string.IsNullOrEmpty(sortBy))
            {
                requestExports = sortBy.ToLower() switch
                {
                    "status" => requestExports.OrderBy(re => statusPriority.ContainsKey(re.Status) ? statusPriority[re.Status] : 99).ToList(),
                    "requestdate_desc" => requestExports.OrderByDescending(re => re.RequestDate).ToList(),
                    "requestdate_asc" => requestExports.OrderBy(re => re.RequestDate).ToList(),
                    _ => requestExports
                };
            }

            return requestExports.Select(re =>
            {
                var (warehouseId, warehouseName) = tempExportDict.ContainsKey(re.OrderId)
                    ? GetPrimaryWarehouse(re.OrderId, tempExportDict[re.OrderId])
                    : (0, "Unknown");

                return new RequestExportDto
                {
                    RequestExportId = re.RequestExportId,
                    OrderId = re.OrderId,
                    AgencyName = re.RequestedByAgency?.AgencyName ?? "Unknown",
                    RequestDate = re.RequestDate,
                    Status = re.Status,
                    Note = re.Note,
                    RequestExportCode = re.RequestExportCode,
                    WarehouseId = warehouseId,
                    WarehouseName = warehouseName,
                    RequestExportDetails = re.RequestExportDetails.Select(red => new RequestExportDetailDto
                    {
                        RequestExportDetailId = red.RequestItemId,
                        ProductId = red.ProductId,
                        ProductName = red.Product?.ProductName ?? "N/A",
                        Unit = red.Unit,
                        Price = red.SellingPrice,
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
                        : new List<TemporaryStockExportDto>()
                };
            }).ToList();
        }


        public async Task<RequestExportDto?> GetRequestExportByIdForSalesAsync(int requestId, Guid salesUserId)
        {
            var re = await _requestExportRepository.GetRequestExportByIdAsync(requestId, salesUserId);
            if (re == null) return null;

            var tempExports = await _temporaryWarehouseRepository.GetByOrderIdAsync(re.OrderId);
            var (warehouseId, warehouseName) = GetPrimaryWarehouse(re.OrderId, tempExports);

            return new RequestExportDto
            {
                RequestExportId = re.RequestExportId,
                OrderId = re.OrderId,
                AgencyName = re.RequestedByAgency?.AgencyName ?? "Unknown",
                RequestDate = re.RequestDate,
                Status = re.Status,
                Note = re.Note,
                WarehouseId = warehouseId,
                WarehouseName = warehouseName,
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
                TemporaryStockExportDetails = tempExports.Select(tse => new TemporaryStockExportDto
                {
                    WarehouseId = tse.WarehouseId,
                    ProductId = tse.ProductId,
                    BatchId = tse.BatchId,
                    Quantity = tse.Quantity
                }).ToList()
            };
        }

        private (long warehouseId, string warehouseName) GetPrimaryWarehouse(Guid orderId, List<TemporaryStockExport> tempExports)
        {
            if (tempExports == null || !tempExports.Any())
                return (0, "Unknown");

            var mainWarehouse = tempExports
                .Where(x => x.OrderId == orderId)
                .GroupBy(x => x.WarehouseId)
                .Select(g => new
                {
                    WarehouseId = g.Key,
                    TotalQuantity = g.Sum(x => x.Quantity),
                    WarehouseName = g.FirstOrDefault()?.Warehouse?.WarehouseName ?? "Unknown"
                })
                .OrderByDescending(x => x.TotalQuantity)
                .FirstOrDefault();

            return mainWarehouse != null
                ? (mainWarehouse.WarehouseId, mainWarehouse.WarehouseName)
                : (0, "Unknown");
        }



    }
}
