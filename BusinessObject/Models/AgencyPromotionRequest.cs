using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.Models
{
    public class AgencyPromotionRequest
    {
        public Guid AgencyPromotionRequestId { get; set; }
        public long AgencyId { get; set; }
        public long CurrentLevelId { get; set; }
        public long SuggestedLevelId { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalScore { get; set; }
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReviewedAt { get; set; }
        public Guid ReviewedBy { get; set; }

        public AgencyAccount Agency { get; set; }
    }

}
