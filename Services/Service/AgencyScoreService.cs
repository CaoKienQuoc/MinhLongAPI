using BusinessObject.Models;
using Repo.IRepository;
using Repo.Repository;
using Services.IService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Service
{
    public class AgencyScoreService : IAgencyScoreService
    {
        private readonly IAgencyScoreHistoryRepository _scoreRepo;
        private readonly IAgencyPromotionRequestRepository _promotionRepo;
        private readonly IAgencyAccountRepository _accountRepo;

        public AgencyScoreService(
            IAgencyScoreHistoryRepository scoreRepo,
            IAgencyPromotionRequestRepository promotionRepo,
            IAgencyAccountRepository accountRepo)
        {
            _scoreRepo = scoreRepo;
            _promotionRepo = promotionRepo;
            _accountRepo = accountRepo;
        }

        public async Task AddScoreAsync(long agencyId, int scoreChange, string reason)
        {
            await _scoreRepo.AddAsync(new AgencyScoreHistory
            {
                AgencyId = agencyId,
                ScoreChange = scoreChange,
                Reason = reason
            });
            await EvaluatePromotionAsync(agencyId);
        }

        public async Task EvaluatePromotionAsync(long agencyId)
        {
            var score = await _scoreRepo.GetTotalScoreAsync(agencyId);
            var agency = await _accountRepo.GetByIdAsync(agencyId);

            if (agency == null)
                throw new Exception("Không tìm thấy đại lý.");

            if (agency.AgencyAccountLevels == null || !agency.AgencyAccountLevels.Any())
                throw new Exception("Đại lý chưa có dữ liệu cấp độ.");

            // 🧠 Lấy cấp hiện tại dựa trên bản ghi mới nhất theo ChangeDate
            var currentLevel = agency.AgencyAccountLevels
                .OrderByDescending(l => l.ChangeDate)
                .FirstOrDefault();

            if (currentLevel != null && currentLevel.LevelId == 3 && score >= 100)
            {
                var hasPending = await _promotionRepo.IsPendingRequestExistAsync(agencyId);
                if (!hasPending)
                {
                    await _promotionRepo.CreateAsync(new AgencyPromotionRequest
                    {
                        AgencyId = agencyId,
                        CurrentLevelId = (int)currentLevel.LevelId,
                        SuggestedLevelId = 2,
                        TotalScore = score,
                        Status = "Pending",
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }
        }

    }
}
