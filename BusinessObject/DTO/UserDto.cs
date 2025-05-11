using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.DTO
{
    public class UserDto
    {
        public Guid UserId { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string Email { get; set; }
        public string UserType { get; set; }
        public string Phone { get; set; }
        public bool Status { get; set; }
        public bool VerifyEmail { get; set; }

        public string FullName { get; set; }
        public string Position { get; set; }
        public string Department { get; set; }

        // Thông tin chi tiết cho AGENCY
        public string AgencyName { get; set; }

        // Địa chỉ chung cho cả EMPLOYEE và AGENCY
        public string Address { get; set; }

        public List<ContractDto> Contracts { get; set; } = new List<ContractDto>();
    }
}
