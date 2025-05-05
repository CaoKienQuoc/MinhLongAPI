using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.IService
{
    public interface IAgencyScoreService
    {
        Task AddScoreAsync(long agencyId, int scoreChange, string reason);
        Task EvaluatePromotionAsync(long agencyId);
    }
}
