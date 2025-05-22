using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.DTO
{
    public class AgencyPromotionRequestDto
    {
        public Guid AgencyPromotionRequestId { get; set; }
        public long AgencyId { get; set; }
        public string AgencyName { get; set; }
        public long CurrentLevelId { get; set; }
        public long SuggestedLevelId { get; set; }
        public long TotalScore { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public Guid ReviewedBy { get; set; }
    }
}
