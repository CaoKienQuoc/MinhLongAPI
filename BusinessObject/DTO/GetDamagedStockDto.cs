using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.DTO
{
    public class GetDamagedStockDto
    {
        public long DamagedStockId { get; set; }
        public long WarehouseId { get; set; }
        public string WarehouseName { get; set; }
        public long ProductId { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Reason { get; set; }
        public string Status { get; set; }
        public string? OrderCode { get; set; }
        public string? BatchCode { get; set; }
    }
}
