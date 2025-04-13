using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.DTO.RequestExport
{
    public class TemporaryStockExportDto
    {
        public long WarehouseId { get; set; }
        public long ProductId { get; set; }
        public long BatchId { get; set; }
        public long Quantity { get; set; }
    }

}
