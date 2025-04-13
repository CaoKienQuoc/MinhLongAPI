using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.Models
{
    public class RequestExport
    {
        [Key]
        public int RequestExportId { get; set; }

        public string RequestExportCode { get; set; }

        public long RequestedByAgencyId { get; set; }

        public DateTime RequestDate { get; set; }

        public string Status { get; set; } // Pending, Approved, Rejected

        [ForeignKey("ApprovedBy")]
        public long? ApprovedBy { get; set; }
        public Employee ApprovedByEmployee { get; set; }

        public DateTime? ApprovedDate { get; set; }
        public string Note { get; set; }

        // ✅ Định nghĩa quan hệ 1-1 với Order
        [ForeignKey("Order")]
        public Guid OrderId { get; set; }
        public virtual Order Order { get; set; } // ✅ Đảm bảo đây là `virtual`

        // ✅ Thêm dòng này:
        public AgencyAccount RequestedByAgency { get; set; }

        public ICollection<RequestExportDetail> RequestExportDetails { get; set; }
        public ICollection<WarehouseRequestExport> WarehouseRequestExports { get; set; }
    }

}
