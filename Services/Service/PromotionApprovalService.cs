using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessObject.Models;
using Repo.IRepository;
using Repo.Repository;
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

            // ✅ Lấy LevelId từ SuggestedLevelId
            long suggestedLevelId = request.SuggestedLevelId;

            // ✅ Lấy DiscountPercentage từ bảng AgencyLevel
            var levelInfo = await _levelRepo.GetLevelByIdAsync(suggestedLevelId);
            if (levelInfo == null)
                throw new Exception($"Không tìm thấy thông tin cho LevelId = {suggestedLevelId}.");

            // ✅ Lấy bản ghi cấp hiện tại của đại lý
            var agencyLevel = await _levelRepo.GetLatestLevelByAgencyIdAsync(request.AgencyId);
            if (agencyLevel == null)
                throw new Exception("Không tìm thấy bản ghi cấp của đại lý.");

            // ✅ Cập nhật cấp & gán DiscountPercentage
            agencyLevel.LevelId = suggestedLevelId;
            agencyLevel.OrderDiscount = levelInfo.DiscountPercentage.GetValueOrDefault();
            agencyLevel.ChangeDate = DateTime.Now;

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
