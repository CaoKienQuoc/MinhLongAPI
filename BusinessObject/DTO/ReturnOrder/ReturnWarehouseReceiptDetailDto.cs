using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.DTO.ReturnOrder
{
    public class ReturnWarehouseReceiptDetailDto
    {
        public long ReturnWarehouseReceiptDetailId { get; set; }
        public long ProductId { get; set; }
        public int Quantity { get; set; }
        public long? BatchId { get; set; }
        public string Reason { get; set; }
    }
}
