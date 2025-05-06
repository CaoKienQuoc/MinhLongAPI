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
        public int CurrentLevelId { get; set; }
        public int SuggestedLevelId { get; set; }
        public int TotalScore { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public Guid ReviewedBy { get; set; }
    }
}
