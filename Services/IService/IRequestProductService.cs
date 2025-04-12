using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessObject.DTO;
using BusinessObject.DTO.Product;
using BusinessObject.Models;

namespace Services.IService
{
    public interface IRequestProductService
    {
        //Task<IEnumerable<RequestProduct>> GetAllRequestsAsync();
        Task<List<RequestProductDto>> GetAllRequestsAsync();
        //Task<RequestProduct> GetRequestByIdAsync(Guid id);
        Task<RequestProductDto> GetRequestByIdAsync(Guid id);
        Task CreateRequestAsync(RequestProduct requestProduct, List<RequestProductDetail> requestDetails, Guid userId);
        Task ApproveRequestAsync(Guid requestId);

        //Task<List<RequestProduct>> GetRequestProductsByAgencyIdAsync(long agencyId);
        Task<List<RequestProductDto>> GetRequestProductsByAgencyIdAsync(long agencyId);

        //Task<List<RequestProduct>> GetRequestProductsByIdAsync(Guid requestId);

        Task<bool> CancelRequestAsync(Guid requestId, long approvedBy);
    }
}
