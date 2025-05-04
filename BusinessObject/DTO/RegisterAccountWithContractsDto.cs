using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.DTO
{
    public class RegisterAccountWithContractsDto
    {
        public int RegisterId { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string UserType { get; set; }
        public string? FullName { get; set; }
        public string? Position { get; set; }
        public string? Department { get; set; }
        public string? AgencyName { get; set; }
        public string Street { get; set; }
        public string WardName { get; set; }
        public string DistrictName { get; set; }
        public string ProvinceName { get; set; }
        public bool IsApproved { get; set; }
        public string AccountRegisterStatus { get; set; }

        public List<ContractDto> Contracts { get; set; } = new();
    }
}
