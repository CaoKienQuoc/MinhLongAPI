using BusinessObject.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repo.IRepository
{
    public interface IRequestExportRepository
    {
        Task<List<RequestExport>> GetAllRequestExportsAsync();

        Task<List<RequestExport>> GetRequestExportsBySalesUserIdAsync(Guid salesUserId);

        Task<RequestExport?> GetRequestExportByIdAsync(int requestId, Guid salesUserId);

        Task AddExportAsync(RequestExport export);
        Task AddExportDetailsAsync(List<RequestExportDetail> exportDetails);
        Task<RequestExport> GetRequestExportById(int requestId);
        Task SaveChangesAsync();
        Task UpdateExportAsync(RequestExport export);

        Task<RequestExport> GetRequestExportByIdAsync(int requestExportId);
        Task UpdateRequestExportAsync(RequestExport requestExport);

        Task<Guid?> GetOrderIdByRequestExportIdAsync(int requestExportId);
    }

}
