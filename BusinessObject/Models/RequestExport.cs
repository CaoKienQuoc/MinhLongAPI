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

        public string Note { get; set; }

        // ✅ Định nghĩa quan hệ 1-1 với Order
        [ForeignKey("Order")]
        public Guid OrderId { get; set; }
        public virtual Order Order { get; set; } // ✅ Đảm bảo đây là `virtual`

        // ✅ Thêm dòng này:
        public AgencyAccount RequestedByAgency { get; set; }

        public ICollection<RequestExportDetail> RequestExportDetails { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal Discount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal FinalPrice { get; set; }
    }

}
