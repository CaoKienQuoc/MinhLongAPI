using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessObject.Models;

namespace Repo.IRepository
{
    public interface IRequestProductRepository
    {
        Task<IEnumerable<RequestProduct>> GetAllRequestsAsync();
        Task<RequestProduct> GetPendingRequestByAgencyAsync(long agencyId);
        Task<RequestProduct> GetRequestByIdAsync(Guid id);
        Task AddRequestAsync(RequestProduct requestProduct);
        Task UpdateRequestAsync(RequestProduct requestProduct);
        Task SaveChangesAsync();
        Task<RequestProduct> GetRequestProductByRequestIdAsync(Guid requestId);
        //Task<List<RequestProduct>> GetRequestProductByIdAsync(Guid requestId);

        Task<List<RequestProduct>> GetRequestProductAgencyIdAsync(long agencyId);

        Task<string> GenerateRequestCodeAsync();

        Task<string> GenerateOrderCodeAsync();

       

    }
}
