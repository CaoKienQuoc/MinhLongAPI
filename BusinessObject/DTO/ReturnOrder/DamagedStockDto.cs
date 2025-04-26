using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.DTO.ReturnOrder
{
    public class DamagedStockDto
    {
        public long DamagedStockId { get; set; }
        public long WarehouseId { get; set; }
        public string WarehouseName { get; set; }
        public long ProductId { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public DateTime CreatedAt { get; set; }
        public long BatchId { get; set; }
    }
}
