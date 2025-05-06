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

        public async Task ApprovePromotionAsync(Guid promotionRequestId)
        {
            var request = await _promotionRepo.GetByIdAsync(promotionRequestId);
            if (request == null || request.Status != "Pending")
                throw new Exception("Yêu cầu thăng hạng không hợp lệ hoặc đã được duyệt.");

            // ✅ Thêm bản ghi cấp mới cho agency
            var newLevel = new AgencyAccountLevel
            {
                AgencyId = request.AgencyId,
                LevelId = request.SuggestedLevelId,
                ChangeDate = DateTime.Now
            };
            await _levelRepo.AddAsync(newLevel); // <-- Cần thêm method AddAsync()

            // ✅ Cập nhật trạng thái request
            request.Status = "Approved";
            await _promotionRepo.UpdateAsync(request);

            // ✅ Lưu thay đổi
            await _levelRepo.SaveAsync();
            await _promotionRepo.SaveChangesAsync();
        }

    }

}
