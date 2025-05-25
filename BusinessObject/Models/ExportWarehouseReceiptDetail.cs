using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.Models
{
    public class ExportWarehouseReceiptDetail
    {
        [Key]
        public long ExportWarehouseReceiptDetailId { get; set; }

        [Required]
        public long ExportWarehouseReceiptId { get; set; }
        [ForeignKey("ExportWarehouseReceiptId")]
        public ExportWarehouseReceipt ExportWarehouseReceipt { get; set; }

        [Required]
        public long WarehouseProductId { get; set; } // ✅ Cần – xác định chính xác lô tồn kho
        [ForeignKey("WarehouseProductId")]
        public WarehouseProduct WarehouseProduct { get; set; }

        [Required]
        public long ProductId { get; set; } // ✅ Cần
        [ForeignKey("ProductId")]
        public Product Product { get; set; }

        [Required]
        public string ProductName { get; set; } // ✅ Cần – lưu tên sản phẩm tại thời điểm xuất

        [Required]
        public string BatchNumber { get; set; } // ✅ Cần – theo dõi lô hàng

        [Required]
        public int Quantity { get; set; }       // ✅ Cần

        [Required]
        public decimal UnitPrice { get; set; }  // ✅ Cần

        [Required]
        public decimal TotalProductAmount { get; set; } // ✅ Cần

        [Required]
        public DateTime ExpiryDate { get; set; } // ✅ Cần – dùng cho sản phẩm có hạn dùng

        public long BatchId { get; set; }

        [ForeignKey("BatchId")]
        public Batch Batch { get; set; }


    }

}
