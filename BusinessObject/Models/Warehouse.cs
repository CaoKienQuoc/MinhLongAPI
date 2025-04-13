using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.Models
{
    public class Warehouse
    {
        [Key]
        public long WarehouseId { get; set; }

        [Required]
        public string WarehouseName { get; set; }

        [Required]
        public Guid UserId { get; set; } // Người tạo Warehouse

        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        [Required]
        public int AddressId { get; set; }  // Liên kết với bảng Address

        [ForeignKey("AddressId")]
        public virtual Address Address { get; set; }

        // Các mối quan hệ bổ sung
        public ICollection<ExportTransaction> ExportTransactions { get; set; } // Một kho có thể có nhiều giao dịch xuất kho
        public ICollection<WarehouseReceipt> WarehouseReceipts { get; set; } // Một kho có thể có nhiều phiếu nhập kho
        public ICollection<ExportWarehouseReceipt> ExportWarehouseReceipts { get; set; }// 🔹 Thêm danh sách ExportWarehouseReceipts (KHÔNG phải ExportWarehouseReceiptDetails)
        public ICollection<WarehouseProduct> WarehouseProducts { get; set; } // Một kho có thể có nhiều sản phẩm kho                                                                            // 🔹 Mới thêm: Mối quan hệ với WarehouseLedger
        public ICollection<WarehouseLedger> WarehouseLedgers { get; set; } // Một kho có nhiều bản ghi nhật ký kho

        public string Note { get; set; } // Ghi chú cho kho

        public virtual ICollection<TemporaryStockExport> TemporaryStockExports { get; set; } = new List<TemporaryStockExport>();
    }

}
