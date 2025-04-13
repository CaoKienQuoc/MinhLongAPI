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

        public long WarehouseTransferRequestId { get; set; }
        [ForeignKey("WarehouseTransferRequestId")]
        public WarehouseTransferRequest WarehouseTransferRequest { get; set; }

        public long ProductId { get; set; }
        [ForeignKey("ProductId")]
        public Product Product { get; set; }

        public long? BatchId { get; set; } // Optional nếu có tracking batch
        [ForeignKey("BatchId")]
        public Batch? Batch { get; set; }

        public int Quantity { get; set; }
    }

}
