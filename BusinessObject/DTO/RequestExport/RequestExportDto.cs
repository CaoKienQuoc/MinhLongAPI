using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.DTO.RequestExport
{
    public class RequestExportDto
    {
        public int RequestExportId { get; set; }
        public Guid OrderId { get; set; }
        public string RequestExportCode { get; set; }
        public string AgencyName { get; set; }
        public string ApprovedByName { get; set; }

        public DateTime RequestDate { get; set; }
        //public long RequestedBy { get; set; }
        //public long? ApprovedBy { get; set; }
        public string Status { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public string Note { get; set; }
        public List<RequestExportDetailDto> RequestExportDetails { get; set; }
        // ✅ Thêm dòng này:
        public List<TemporaryStockExportDto> TemporaryStockExportDetails { get; set; }
    }
}
