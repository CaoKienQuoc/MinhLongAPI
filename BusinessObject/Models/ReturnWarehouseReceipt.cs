using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.Models
{
    public class ReturnWarehouseReceipt
    {
        [Key]
        public long ReturnWarehouseReceiptId { get; set; }

        // ✅ Mã phiếu nhập trả hàng (ví dụ: RR-20250426210130)
        [Required]
        public string ReceiptCode { get; set; }

        // ✅ Ngày lập phiếu
        [Required]
        public DateTime ReceiptDate { get; set; }

        // ✅ Ngày tạo bản ghi
        [Required]
        public DateTime CreatedAt { get; set; }

        // ✅ Người tạo phiếu (userId)
        [Required]
        public Guid CreatedBy { get; set; }

        // ✅ Người tạo phiếu (userId)
        [Required]
        public Guid ApprovedBy { get; set; }

        public Guid ReturnRequestId { get; set; }
        [ForeignKey(nameof(ReturnRequestId))]
        public ReturnRequest ReturnRequest { get; set; }

        public long WarehouseId { get; set; } // kho huỷ
        [ForeignKey(nameof(WarehouseId))]
        public Warehouse Warehouse { get; set; }

        //public string? Note { get; set; }
        public string Status { get; set; } // Trạng thái phiếu nhập (Pending, Completed, Cancelled)

        public string? Reason { get; set; }       // Lý do từ chối
        public ICollection<ReturnWarehouseReceiptDetail> Details { get; set; }
    }

}
