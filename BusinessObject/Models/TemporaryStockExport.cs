using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.Models
{
    public class TemporaryStockExport
    {
        public long TemporaryStockExportId { get; set; }

        public long ProductId { get; set; }
        public Product Product { get; set; }

        public long WarehouseId { get; set; }
        public Warehouse Warehouse { get; set; }

        public long Quantity { get; set; }

        public Guid OrderId { get; set; }
        public Order Order { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsReverted { get; set; } = false;
    }

}
