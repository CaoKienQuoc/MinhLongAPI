using BusinessObject.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repo.IRepository
{
    public interface IExportRepository
    {
        Task<List<RequestExport>> GetAllRequestExportsAsync();
        Task AddExportAsync(RequestExport export);
        Task AddExportDetailsAsync(List<RequestExportDetail> exportDetails);
        Task<RequestExport> GetRequestExportById(int requestId);
        Task SaveChangesAsync();
        Task UpdateExportAsync(RequestExport export);
    }

}
