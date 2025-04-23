using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.Models
{
    public class WarehouseReceipt
    {
        [Key]
        public long WarehouseReceiptId { get; set; } // Mã chứng từ, khóa chính

        [Required]
        public string DocumentNumber { get; set; } // Số chứng từ

        [Required]
        public DateTime DocumentDate { get; set; } // Ngày chứng từ

        [Required]
        public long WarehouseId { get; set; } // Kho nhập
        public Warehouse Warehouse { get; set; } // Liên kết với bảng kho

        [Required]
        public string ImportType { get; set; } // Loại nhập (Nhập sản xuất, Nhập chuyển, Nhập trả)

        public string Supplier { get; set; } // Nhà cung cấp

        public DateTime DateImport { get; set; } // Ngày nhập hàng thực tế

        // Dữ liệu từ bảng WarehouseReceiptDetail được đưa vào đây
        public int TotalQuantity { get; set; } // Tổng số lượng nhập kho
        public decimal TotalPrice { get; set; } // Tổng giá tiền nhập kho

        // Danh sách các lô hàng (1 WarehouseReceipt có nhiều Batch)
        public string BatchesJson { get; set; }

        public string Note { get; set; } = "nothing";

        public bool IsApproved { get; set; } = false;  // Mặc định chưa duyệt
    }

}