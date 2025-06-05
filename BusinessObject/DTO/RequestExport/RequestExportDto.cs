using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.DTO.RequestExport
{
    public class RequestExportDto
    {
        public int RequestExportId { get; set; }
        public Guid OrderId { get; set; }
        public string OrderCode { get; set; }
        public string RequestExportCode { get; set; }
        public string AgencyName { get; set; }
        public string ApprovedByName { get; set; }

        public long WarehouseId { get; set; }
        public string WarehouseName { get; set; }
        public DateTime RequestDate { get; set; }
        //public long RequestedBy { get; set; }
        //public long? ApprovedBy { get; set; }
        public string Status { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public string Note { get; set; }
        public List<RequestExportDetailDto> RequestExportDetails { get; set; }
        // ✅ Thêm dòng này:
        public List<TemporaryStockExportDto> TemporaryStockExportDetails { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal Discount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal FinalPrice { get; set; }
        public string? Reason { get; set; }
    }
}
