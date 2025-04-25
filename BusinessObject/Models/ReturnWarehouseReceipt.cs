using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.Models
{
    public class ReturnWarehouseReceipt
    {
        [Key]
        public long ReturnWarehouseReceiptId { get; set; }

        public DateTime ReceiptDate { get; set; }

        public long ReturnRequestId { get; set; }
        [ForeignKey(nameof(ReturnRequestId))]
        public ReturnRequest ReturnRequest { get; set; }

        public long WarehouseId { get; set; } // kho huỷ
        [ForeignKey(nameof(WarehouseId))]
        public Warehouse Warehouse { get; set; }

        public string Note { get; set; }

        public ICollection<ReturnWarehouseReceiptDetail> Details { get; set; }
    }

}
