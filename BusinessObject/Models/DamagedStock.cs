using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.Models
{
    public class DamagedStock
    {
        [Key]
        public long DamagedStockId { get; set; }

        public long WarehouseId { get; set; }
        [ForeignKey(nameof(WarehouseId))]
        public Warehouse Warehouse { get; set; }

        public long ProductId { get; set; }
        [ForeignKey(nameof(ProductId))]
        public Product Product { get; set; }

        public long? BatchId { get; set; } // ✅ Thay vì BatchCode
        [ForeignKey(nameof(BatchId))]
        public Batch Batch { get; set; }   // ✅ Entity Batch
        public int Quantity { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string Reason { get; set; } // Lý do hủy hàng

        public string Status { get; set; } // Trạng thái (Pending, Completed, Cancelled)
    }

}
