using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessObject.DTO.Warehouse;
using BusinessObject.Models;

namespace Services.IService
{
    public interface IWarehouseRequestExportService
    {
        /*Task<WarehouseRequestExport> CreateWarehouseRequestExportAsync(long warehouseId, int requestExportId);*/
        Task<List<WarehouseRequestExport>> CreateWarehouseRequestExportAsync(long warehouseId, int requestExportId);

        Task<bool> ApproveRequestAsync(int warehouseRequestExportId, int quantityApproved, Guid approvedBy);

        Task<bool> ApproveAllRequestsByWarehouseAsync(int warehouseId, int requestExportId, Dictionary<int, int> quantitiesApproved, Guid approvedBy);

        Task<List<WarehouseRequestExportDtoResponse>> GetByWarehouseIdAsync(long warehouseId, string? sortBy = null);


        Task<WarehouseRequestExportDtoResponse> GetByWarehouseRequestExportIdAsync(int warehouseRequestExportId);




    }
}
