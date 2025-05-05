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

            if (agency.AgencyAccountLevels.Count == 3 && score >= 100)
            {
                var hasPending = await _promotionRepo.IsPendingRequestExistAsync(agencyId);
                if (!hasPending)
                {
                    await _promotionRepo.CreateAsync(new AgencyPromotionRequest
                    {
                        AgencyId = agencyId,
                        CurrentLevelId = 3,
                        SuggestedLevelId = 2,
                        TotalScore = score
                    });
                }
            }
        }
    }
}
