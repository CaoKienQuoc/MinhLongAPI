using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.Models
{
    public class ReturnRequest
    {
        [Key]
        public Guid ReturnRequestId { get; set; }

        [Required]
        public Guid CreatedByUserId { get; set; } // đại lý

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public string Status { get; set; } // Pending, Approved, Rejected, Imported

        public string? Note { get; set; } // lý do chung (nếu có)

        public Guid OrderId { get; set; }
        [ForeignKey(nameof(OrderId))]
        public Order Order { get; set; }

        public ICollection<ReturnRequestDetail> Details { get; set; }


    }

}
