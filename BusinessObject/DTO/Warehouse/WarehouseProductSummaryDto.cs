using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.DTO.Warehouse
{
    public class WarehouseProductSummaryDto
    {
        public long ProductId { get; set; }
        public string ProductName { get; set; }
        public int TotalQuantity { get; set; }
        public string Status { get; set; }
    }
}
