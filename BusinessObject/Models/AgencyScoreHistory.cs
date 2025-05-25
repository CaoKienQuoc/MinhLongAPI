using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.Models
{
    public class AgencyScoreHistory
    {
        public Guid AgencyScoreHistoryId { get; set; }
        public long AgencyId { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal ScoreChange { get; set; }
        public string Reason { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public AgencyAccount Agency { get; set; }
    }


}
