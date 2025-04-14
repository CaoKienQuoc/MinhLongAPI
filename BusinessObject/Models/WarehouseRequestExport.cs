using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.Models
{
    public class WarehouseRequestExport
    {
        [Key]
        public int WarehouseRequestExportId { get; set; }


        [ForeignKey("RequestExport")]
        public int RequestExportId { get; set; }
        public RequestExport RequestExport { get; set; }

        [ForeignKey("Warehouse")]
        public long WarehouseId { get; set; }
        public Warehouse Warehouse { get; set; }

        [ForeignKey("Product")]
        public long ProductId { get; set; }
        public Product Product { get; set; }

        public int QuantityRequested { get; set; }
        public int RemainingQuantity { get; set; }

        public string Status { get; set; } = "Pending";

        [Required]
        public Guid RequestedBy { get; set; }
        [ForeignKey("RequestedBy")]
        public User RequestedByUser { get; set; }

        // ✅ Đại lý yêu cầu (gắn với Order/RequestExport)
        [Required]
        public long RequestedByAgencyId { get; set; }
        [ForeignKey("RequestedByAgencyId")]
        public AgencyAccount RequestedByAgency { get; set; }


    }

}
