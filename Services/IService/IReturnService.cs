using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessObject.DTO.ReturnOrder;
using BusinessObject.Models;
using Microsoft.AspNetCore.Http;

namespace Services.IService
{
    public interface IReturnService
    {
        Task<ReturnRequest> CreateReturnRequestWithImagesAsync(Guid orderId, Guid orderDetailId, int quantity, string reason, string? note, Guid userId, List<IFormFile> images);
        Task ApproveReturnRequestAsync(Guid id);
        Task<List<ReturnRequest>> GetPendingReturnsAsync();

    }

}
