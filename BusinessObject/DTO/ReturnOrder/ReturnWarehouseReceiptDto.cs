using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.DTO.ReturnOrder
{
    public class ReturnWarehouseReceiptDto
    {
        public long ReturnWarehouseReceiptId { get; set; }
        public string ReceiptCode { get; set; }
        public DateTime ReceiptDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedByUserName { get; set; }
        public Guid ReturnRequestId { get; set; }
        public long WarehouseId { get; set; }
        public string Note { get; set; }
        public string Status { get; set; }
        public List<ReturnWarehouseReceiptDetailDto> Details { get; set; }
    }
}
