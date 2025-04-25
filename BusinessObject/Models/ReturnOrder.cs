using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.Models
{
    public class ReturnOrder
    {
        [Key]
        public long ReturnOrderId { get; set; }

        [Required]
        public long OrderId { get; set; }
        [ForeignKey(nameof(OrderId))]
        public Order Order { get; set; }

        [Required]
        public long WarehouseId { get; set; }
        [ForeignKey(nameof(WarehouseId))]
        public Warehouse Warehouse { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public Guid CreatedBy { get; set; }

        [Required, StringLength(1000)]
        public string Reason { get; set; }

        [StringLength(50)]
        public string Status { get; set; } = "PendingSale";

        public ICollection<ReturnOrderDetail> Details { get; set; }
        public ICollection<ReturnOrderImage> Images { get; set; }
    }
}
