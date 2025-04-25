using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessObject.DTO.AgencyLevel;
using BusinessObject.Models;
using Repo.IRepository;
using Services.IService;

namespace Services.Service
{
    public class AgencyLevelService : IAgencyLevelService
    {
        private readonly IAgencyLevelRepository _repo;
        private readonly IAgencyAccountRepository _accountRepo;

        public AgencyLevelService(IAgencyLevelRepository repo, IAgencyAccountRepository accountRepo)
        {
            _repo = repo;
            _accountRepo = accountRepo;
        }

        public async Task<IEnumerable<AgencyLevel>> GetAllLevelsAsync()
        {
            return await _repo.GetAllAsync();
        }

        public async Task<AgencyLevel?> GetLevelByIdAsync(long id)
        {
            return await _repo.GetByIdAsync(id);
        }

        public async Task CreateLevelAsync(CreateAgencyLevelDto dto)
        {
            var level = new AgencyLevel
            {
                LevelName = dto.LevelName,
                DiscountPercentage = dto.DiscountPercentage,
                CreditLimit = dto.CreditLimit,
                PaymentTerm = dto.PaymentTerm
            };
            await _repo.AddAsync(level);
        }

        public async Task UpdateLevelAsync(long id, UpdateAgencyLevelDto dto)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null)
                throw new Exception("Agency level not found");

            existing.LevelName = dto.LevelName;
            existing.DiscountPercentage = dto.DiscountPercentage;
            existing.CreditLimit = dto.CreditLimit;
            existing.PaymentTerm = dto.PaymentTerm;

            await _repo.UpdateAsync(existing);
        }


        public async Task DeleteLevelAsync(long id)
        {
            await _repo.DeleteAsync(id);
        }

        public async Task<CurrentAgencyLevelDto?> GetCurrentLevelByUserIdAsync(Guid userId)
        {
            var agency = await _accountRepo.GetByUserIdWithLevelsAsync(userId);

            if (agency == null || agency.AgencyAccountLevels == null || !agency.AgencyAccountLevels.Any())
                return null;

            var latestLevel = agency.AgencyAccountLevels
                .OrderByDescending(al => al.ChangeDate)
                .FirstOrDefault();

            if (latestLevel?.Level == null)
                return null;

            return new CurrentAgencyLevelDto
            {
                LevelName = latestLevel.Level.LevelName,
                CreditLimit = latestLevel.Level.CreditLimit,
                PaymentTerm = latestLevel.Level.PaymentTerm,
                DiscountPercentage = latestLevel.Level.DiscountPercentage
            };
        }

        public async Task<IEnumerable<object>> GetAgencyCountByLevelAsync()
        {
            var allLevels = await _repo.GetAllAsync();

            return allLevels.Select(l => new
            {
                LevelName = l.LevelName,
                AgencyCount = l.AgencyAccountLevels?.Count ?? 0
            });
        }

    }


}
