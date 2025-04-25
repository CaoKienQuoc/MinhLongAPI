using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.Models
{
    public class ExportWarehouseReceipt
    {
        [Key]
        public long ExportWarehouseReceiptId { get; set; }

        [Required]
        public string DocumentNumber { get; set; }           // ✅ Cần – mã chứng từ (PXK-001)

        [Required]
        public DateTime DocumentDate { get; set; }           // ✅ Cần – ngày lập phiếu

        [Required]
        public DateTime ExportDate { get; set; }             // ✅ Cần – ngày thực tế xuất

        [Required]
        public string ExportType { get; set; }               // ✅ Cần – "SaleExport", "InternalTransfer", "Destroyed", etc.

        public int TotalQuantity { get; set; }               // ✅ Cần – tổng số lượng

        public decimal TotalAmount { get; set; }             // ✅ Cần – tổng tiền

        [Required]
        public int RequestExportId { get; set; }             // ✅ Cần – liên kết yêu cầu xuất
        [ForeignKey("RequestExportId")]
        public RequestExport RequestExport { get; set; }

        [Required]
        public string Status { get; set; } = "Pending";      // ✅ Cần – Pending, Completed...

        [Required]
        public long WarehouseId { get; set; }                // ✅ Cần – kho thực hiện xuất
        [ForeignKey("WarehouseId")]
        public Warehouse Warehouse { get; set; }

        public ICollection<ExportWarehouseReceiptDetail> ExportWarehouseReceiptDetails { get; set; }
    }

}
