using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.Models
{
    public class AgencyScoreHistory
    {
        public Guid AgencyScoreHistoryId { get; set; }
        public long AgencyId { get; set; }
        public int ScoreChange { get; set; }
        public string Reason { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public AgencyAccount Agency { get; set; }
    }


}
