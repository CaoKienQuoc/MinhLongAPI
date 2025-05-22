using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.DTO
{
    public class AgencyScoreItemDto
    {
        public long ScoreChange { get; set; }
        public string Reason { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
