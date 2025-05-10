using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessObject.Models;

namespace BusinessObject.Models
{
    [Table("User")]
    public class User
    {
        [Key]
        public Guid UserId { get; set; } = Guid.NewGuid(); // fix GUID

        [Required]
        [StringLength(255)]
        public string Username { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }

        [Required]
        public string UserType { get; set; } // EMPLOYEE or AGENT

        public string Phone { get; set; }
        public bool Status { get; set; } = false;
        public bool VerifyEmail { get; set; } = false;

        public ICollection<UserRole> UserRoles { get; set; }
        // Quan hệ 1-1 với Employee
        public virtual Employee Employee { get; set; }
        public AgencyAccount AgencyAccount { get; set; }

        public ICollection<PaymentHistory> PaymentHistories { get; set; }
        public ICollection<Notification> Notifications { get; set; }

        public ICollection<ChatRoomMember> ChatRoomMemberships { get; set; }
        public ICollection<ChatMessage> SentMessages { get; set; }
        public ICollection<ChatMessage> ReceivedMessages { get; set; }

    }
}
