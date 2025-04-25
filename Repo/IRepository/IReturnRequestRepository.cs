using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessObject.Models;

namespace Repo.IRepository
{
    public interface IReturnRequestRepository
    {
        Task<ReturnRequest> CreateAsync(ReturnRequest request);
        Task<ReturnRequest> GetByIdAsync(Guid id);
        Task<ReturnRequest> GetByIdWithDetailsAsync(Guid id); // ✅ thêm dòng này
        Task<List<ReturnRequest>> GetPendingApprovalAsync();
        Task UpdateStatusAsync(Guid id, string status);

        Task<List<ReturnRequestImage>> AddRangeAsync(List<ReturnRequestImage> images);
    }

}
