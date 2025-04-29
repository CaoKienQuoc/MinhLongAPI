using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.DTO
{
    public class RegisterRequest
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string Phone { get; set; }
        public string UserType { get; set; } // "Agent" hoặc "Employee"

        // Chỉ dành cho Employee
        public string? FullName { get; set; }
        public string? Position { get; set; }
        public string? Department { get; set; }

        // Chỉ dành cho Agent
        public string? AgencyName { get; set; } // ✅ Cho phép null
                                                // ✅ Thêm các thuộc tính mới thay thế cho LocationId
        public string Street { get; set; }
        public string WardName { get; set; }
        public string DistrictName { get; set; }
        public string ProvinceName { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public List<RegisterAccountContractDto> Contracts { get; set; }
    }

}
