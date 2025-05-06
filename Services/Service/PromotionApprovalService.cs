using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessObject.DTO;
using BusinessObject.Models;
using Repo.IRepository;
using Services.IService;

namespace Services.Service
{
    public class PromotionApprovalService : IPromotionApprovalService
    {
        private readonly IAgencyPromotionRequestRepository _promotionRepo;
        private readonly IAgencyAccountLevelRepository _levelRepo;
        private readonly IAgencyAccountRepository _accountRepo;
        private readonly IAgencyScoreHistoryRepository _scoreHistoryRepo;

        public PromotionApprovalService(
            IAgencyPromotionRequestRepository promotionRepo,
            IAgencyAccountLevelRepository levelRepo,
            IAgencyAccountRepository accountRepository,
            IAgencyScoreHistoryRepository scoreHistoryRepository)
        {
            _promotionRepo = promotionRepo;
            _levelRepo = levelRepo;
            _accountRepo = accountRepository;
            _scoreHistoryRepo = scoreHistoryRepository;
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


        public async Task<List<AgencyPromotionRequestDto>> GetRequestsManagedByEmployeeIdAsync(long employeeId)
        {
            var requests = await _promotionRepo.GetRequestsByManagedEmployeeAsync(employeeId);
            return requests.Select(MapToDto).ToList();
        }

        public async Task<AgencyPromotionRequestDto?> GetRequestByIdManagedAsync(Guid requestId, long employeeId)
        {
            var request = await _promotionRepo.GetRequestByIdManagedAsync(requestId, employeeId);
            return request == null ? null : MapToDto(request);
        }

        private AgencyPromotionRequestDto MapToDto(AgencyPromotionRequest request)
        {
            return new AgencyPromotionRequestDto
            {
                AgencyPromotionRequestId = request.AgencyPromotionRequestId,
                AgencyId = request.AgencyId,
                AgencyName = request.Agency?.AgencyName,
                CurrentLevelId = request.CurrentLevelId,
                SuggestedLevelId = request.SuggestedLevelId,
                TotalScore = request.TotalScore,
                Status = request.Status,
                CreatedAt = request.CreatedAt,
                ReviewedAt = request.ReviewedAt,
                ReviewedBy = request.ReviewedBy
            };
        }

        public async Task<AgencyScoreDetailDto> GetScoreDetailByUserIdAsync(Guid userId)
        {
            var agency = await _accountRepo.GetByUserIdAsync(userId);
            if (agency == null)
                throw new Exception("Không tìm thấy thông tin đại lý.");

            var total = await _scoreHistoryRepo.GetTotalScoreByAgencyIdAsync(agency.AgencyId);
            var history = await _scoreHistoryRepo.GetHistoryByAgencyIdAsync(agency.AgencyId);

            return new AgencyScoreDetailDto
            {
                AgencyId = agency.AgencyId,
                AgencyName = agency.AgencyName,
                TotalScore = total,
                ScoreHistory = history.Select(h => new AgencyScoreItemDto
                {
                    ScoreChange = h.ScoreChange,
                    Reason = h.Reason,
                    CreatedDate = h.CreatedDate
                }).ToList()
            };
        }

    }

}
