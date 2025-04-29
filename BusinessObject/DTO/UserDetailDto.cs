using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.DTO
{
    public class UserDetailDto
    {
        public Guid UserId { get; set; }
        public string Username { get; set; }
        public string UserType { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }

        public bool Status { get; set; }
        public bool VerifyEmail { get; set; }

        // Nếu là AGENCY
        public string AgencyLevelName { get; set; }
        public decimal? CreditLimit { get; set; }

        public List<ContractDto> Contracts { get; set; }
    }
}
