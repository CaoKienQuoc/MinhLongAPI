using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessObject.DTO.AgencyLevel;
using BusinessObject.Models;

namespace Services.IService
{
    public interface IAgencyLevelService
    {
        Task<IEnumerable<AgencyLevel>> GetAllLevelsAsync();
        Task<AgencyLevel?> GetLevelByIdAsync(long id);
        Task CreateLevelAsync(CreateAgencyLevelDto dto);
        Task UpdateLevelAsync(long id, UpdateAgencyLevelDto dto);
        Task DeleteLevelAsync(long id);

        Task<CurrentAgencyLevelDto?> GetCurrentLevelByUserIdAsync(Guid userId);

        Task<IEnumerable<object>> GetAgencyCountByLevelAsync();
    }

}
