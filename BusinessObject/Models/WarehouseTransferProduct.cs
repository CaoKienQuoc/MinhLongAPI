using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.Models
{
    public class WarehouseTransferProduct
    {
        [Key]
        public long Id { get; set; }

        [Required]
        public long WarehouseTransferRequestId { get; set; }
        [ForeignKey("WarehouseTransferRequestId")]
        public WarehouseTransferRequest WarehouseTransferRequest { get; set; }

        [Required]
        public long ProductId { get; set; }
        [ForeignKey("ProductId")]
        public Product Product { get; set; }

        public long? BatchId { get; set; } // ✅ Cần – nếu có quản lý batch/lô hàng
        [ForeignKey("BatchId")]
        public Batch? Batch { get; set; }

        [Required]
        public int Quantity { get; set; } // ✅ Cần
    }


}
