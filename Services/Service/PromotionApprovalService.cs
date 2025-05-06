using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessObject.Models;
using Repo.IRepository;
using Services.IService;

namespace Services.Service
{
    public class PromotionApprovalService : IPromotionApprovalService
    {
        private readonly IAgencyPromotionRequestRepository _promotionRepo;
        private readonly IAgencyAccountLevelRepository _levelRepo;

        public PromotionApprovalService(
            IAgencyPromotionRequestRepository promotionRepo,
            IAgencyAccountLevelRepository levelRepo)
        {
            _promotionRepo = promotionRepo;
            _levelRepo = levelRepo;
        }

        public async Task ApprovePromotionAsync(Guid promotionRequestId, Guid userId)
        {
            var request = await _promotionRepo.GetByIdAsync(promotionRequestId);
            if (request == null || request.Status != "Pending")
                throw new Exception("Yêu cầu thăng hạng không hợp lệ hoặc đã được duyệt.");

            // ✅ Lấy bản ghi cấp hiện tại của đại lý
            var agencyLevel = await _levelRepo.GetLatestLevelByAgencyIdAsync(request.AgencyId);
            if (agencyLevel == null)
                throw new Exception("Không tìm thấy bản ghi cấp của đại lý.");

            // ✅ Cập nhật cấp & cộng thêm 5% chiết khấu
            agencyLevel.LevelId = request.SuggestedLevelId;
            agencyLevel.ChangeDate = DateTime.Now;
            agencyLevel.OrderDiscount += 5; // cộng thêm 5%

            await _levelRepo.UpdateAsync(agencyLevel);

            // ✅ Cập nhật trạng thái yêu cầu
            request.Status = "Approved";
            request.ReviewedAt = DateTime.Now;
            request.ReviewedBy = userId;
            await _promotionRepo.UpdateAsync(request);

            // ✅ Lưu thay đổi
            await _levelRepo.SaveAsync();
            await _promotionRepo.SaveChangesAsync();
        }


    }

}
