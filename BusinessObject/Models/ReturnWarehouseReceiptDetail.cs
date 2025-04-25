using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.Models
{
    public class ReturnWarehouseReceiptDetail
    {
        [Key]
        public long ReturnWarehouseReceiptDetailId { get; set; }

        public long ReturnWarehouseReceiptId { get; set; }
        [ForeignKey(nameof(ReturnWarehouseReceiptId))]
        public ReturnWarehouseReceipt ReturnWarehouseReceipt { get; set; }

        public long ProductId { get; set; }
        [ForeignKey(nameof(ProductId))]
        public Product Product { get; set; }

        public int Quantity { get; set; }

        public decimal UnitCost { get; set; }

        public string? BatchCode { get; set; }


    }

}
