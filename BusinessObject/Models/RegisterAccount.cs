using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.Models
{
    public class RegisterAccount
    {
        [Key]
        public int RegisterId { get; set; }

        [Required]
        public string Username { get; set; }

        [Required]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }

        [Required]
        public string Phone { get; set; }

        public string UserType { get; set; } // "EMPLOYEE" hoặc "AGENCY"

        [Column(TypeName = "NVARCHAR(255)")]
        public string? FullName { get; set; }
        public string? Position { get; set; }  // Chỉ dùng cho Employee
        public string? Department { get; set; }  // Chỉ dùng cho Employee

        public string? AgencyName { get; set; } // Chỉ dùng cho Agency

        // ✅ Thay thế `LocationId` bằng thông tin địa chỉ
        public string Street { get; set; }
        public string WardName { get; set; }
        public string DistrictName { get; set; }
        public string ProvinceName { get; set; }

        public bool IsApproved { get; set; } = false;  // Mặc định chưa duyệt

        public string AccountRegisterStatus { get; set; } = "Pending";

        public ICollection<RegisterAccountContract> Contracts { get; set; }
    }
}
