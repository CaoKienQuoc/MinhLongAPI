using BusinessObject.DTO.RequestExport;
using BusinessObject.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.IService
{
    public interface IRequestExportService
    {
        Task<List<RequestExportDto>> GetAllRequestExportsAsync(string? sortBy = null);

        Task<RequestExportDto> GetRequestExportByIdAsync(int requestId);

        Task<List<RequestExportDto>> GetRequestExportsBySalesAsync(Guid salesUserId, string? sortBy = null);

        Task<RequestExportDto?> GetRequestExportByIdForSalesAsync(int requestId, Guid salesUserId);
    }
}
