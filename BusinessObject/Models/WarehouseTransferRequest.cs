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

        // Kho xuất hàng (bắt buộc)
        [Required]
        public long SourceWarehouseId { get; set; }
        [ForeignKey("SourceWarehouseId")]
        public Warehouse SourceWarehouse { get; set; }

        // Kho nhận hàng (bắt buộc)
        [Required]
        public long DestinationWarehouseId { get; set; }
        [ForeignKey("DestinationWarehouseId")]
        public Warehouse DestinationWarehouse { get; set; }

        // Ngày điều phối (xuất hàng)
        public DateTime RequestDate { get; set; } = DateTime.UtcNow;

        // Ghi chú thêm nếu có
        [MaxLength(500)]
        public string? Notes { get; set; }

        [Required, MaxLength(20)]
        public string Status { get; set; } = "Pending"; // Pending, Approved, Completed..

        // Danh sách sản phẩm cần chuyển
        public ICollection<WarehouseTransferProduct> TransferProducts { get; set; } = new List<WarehouseTransferProduct>();
    }



}
