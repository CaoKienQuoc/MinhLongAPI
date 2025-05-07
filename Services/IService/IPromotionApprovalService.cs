using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessObject.DTO;

namespace Services.IService
{
    public interface IPromotionApprovalService
    {
        Task ApprovePromotionAsync(Guid promotionRequestId, Guid userId);

        Task<List<AgencyPromotionRequestDto>> GetRequestsManagedByEmployeeIdAsync(long employeeId);
        Task<AgencyPromotionRequestDto?> GetRequestByIdManagedAsync(Guid requestId, long employeeId);

        Task<AgencyScoreDetailDto> GetScoreDetailByUserIdAsync(Guid userId);
    }
}
