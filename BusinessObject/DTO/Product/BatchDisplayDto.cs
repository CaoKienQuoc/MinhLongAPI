using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.DTO.Product
{
    public class BatchDisplayDto
    {
        public long ProductId { get; set; }                   // Mã sản phẩm
        public string ProductName { get; set; } = string.Empty; // Tên sản phẩm
        public string BatchCode { get; set; } = string.Empty; // Mã lô
        public DateTime ExpiryDate { get; set; }              // Hạn sử dụng
        public DateTime DateOfManufacture { get; set; }              // Hạn sử dụng
        public int Quantity { get; set; }                     // Số lượng
        public decimal TotalAmount { get; set; }
        public decimal? ProfitMarginPercent { get; set; } // Phần trăm lợi nhuận
        public decimal UnitCost { get; set; }             // Giá đã tính
        public decimal SellingPrice { get; set; }             // Giá đã tính
        public string Status { get; set; } = "Đã duyệt";       // Trạng thái
    }

}
