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

        [Required]
        public long SourceWarehouseId { get; set; }            // ✅ Cần
        [ForeignKey("SourceWarehouseId")]
        public Warehouse SourceWarehouse { get; set; }

        [Required]
        public long DestinationWarehouseId { get; set; }       // ✅ Cần
        [ForeignKey("DestinationWarehouseId")]
        public Warehouse DestinationWarehouse { get; set; }

        public int RequestExportId { get; set; }             // ✅ Cần – liên kết yêu cầu xuất (nếu có)

        public DateTime RequestDate { get; set; } = DateTime.UtcNow; // ✅ Cần

        [MaxLength(500)]
        public string? Notes { get; set; }                     // ✅ Tùy – giữ lại để người dùng điền lý do

        [Required, MaxLength(20)]
        public string Status { get; set; } = "Pending";        // ✅ Cần – Pending, Approved, Completed

        public long WarehouseProductId { get; set; } // ✅ Cần – để tracking lại

        // Danh sách sản phẩm cần chuyển
        public ICollection<WarehouseTransferProduct> TransferProducts { get; set; } = new List<WarehouseTransferProduct>();
    }



}
