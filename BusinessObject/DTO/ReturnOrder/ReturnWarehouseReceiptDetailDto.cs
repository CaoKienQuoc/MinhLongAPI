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
        public string ProductName { get; set; }

        public int Quantity { get; set; }
        public long? BatchId { get; set; }
        public string Reason { get; set; }

        public List<ReturnRequestImageDto> Images { get; set; } = new List<ReturnRequestImageDto>();
    }
}
