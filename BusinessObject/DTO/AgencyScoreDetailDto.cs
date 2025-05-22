using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.DTO
{
    public class AgencyScoreDetailDto
    {
        public long AgencyId { get; set; }
        public string AgencyName { get; set; }
        public long TotalScore { get; set; }
        public List<AgencyScoreItemDto> ScoreHistory { get; set; }
    }
}
