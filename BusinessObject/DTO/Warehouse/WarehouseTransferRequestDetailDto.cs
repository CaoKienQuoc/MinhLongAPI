using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.DTO.Warehouse
{
    public class WarehouseTransferRequestDetailDto
    {
        public long Id { get; set; }
        public long SourceWarehouseId { get; set; }
        public string SourceWarehouseName { get; set; }
        public long DestinationWarehouseId { get; set; }
        public string DestinationWarehouseName { get; set; }
        public int RequestExportId { get; set; }
        public DateTime RequestDate { get; set; }
        public string? Notes { get; set; }
        public string Status { get; set; }
        public List<WarehouseTransferProductDto> Products { get; set; }
    }
}
