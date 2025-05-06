using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.Models
{
    public class AgencyPromotionRequest
    {
        public Guid AgencyPromotionRequestId { get; set; }
        public long AgencyId { get; set; }
        public int CurrentLevelId { get; set; }
        public int SuggestedLevelId { get; set; }
        public int TotalScore { get; set; }
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReviewedAt { get; set; }
        public Guid ReviewedBy { get; set; }

        public AgencyAccount Agency { get; set; }
    }

}
