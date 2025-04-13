using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.Models
{
    public class WarehouseTransferRequest
    {
        [Key]
        public long Id { get; set; }

        [Required, MaxLength(50)]

        public long? SourceWarehouseId { get; set; }
        [ForeignKey("SourceWarehouseId")]
        public Warehouse? SourceWarehouse { get; set; }

        [Required]
        public long DestinationWarehouseId { get; set; }
        [ForeignKey("DestinationWarehouseId")]
        public Warehouse DestinationWarehouse { get; set; }

        public DateTime RequestDate { get; set; } = DateTime.UtcNow;

        // Nếu bạn không cần thời gian giao dự kiến có thể bỏ dòng này luôn
        public DateTime? ExpectedDeliveryDate { get; set; }

        [Required]
        public Guid RequestedBy { get; set; }
        [ForeignKey("RequestedBy")]
        public User Requester { get; set; }

        [Required]
        public int RequestExportId { get; set; }
        [ForeignKey("RequestExportId")]
        public RequestExport RequestExport { get; set; }

        [Required, MaxLength(20)]
        public string Status { get; set; } = "Pending"; // Pending, Approved, Completed...

        [MaxLength(500)]
        public string? Notes { get; set; }

        public ICollection<WarehouseTransferProduct> TransferProducts { get; set; } = new List<WarehouseTransferProduct>();
    }


}
