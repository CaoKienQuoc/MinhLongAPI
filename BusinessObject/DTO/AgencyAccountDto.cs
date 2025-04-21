using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.DTO
{
    public class AgencyAccountDto
    {
        public long AgencyId { get; set; }
        public string AgencyName { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
