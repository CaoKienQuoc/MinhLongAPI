using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.DTO.Product
{
    public class BatchResponseDto
    {

        public long? BatchId { get; set; } // ✅ Bổ sung dòng này
        public string BatchCode { get; set; }   // Mã batch
        public long ProductId { get; set; }     // ID sản phẩm
        public string ProductName { get; set; } // Tên sản phẩm
        public string Unit { get; set; }        // Đơn vị tính (Kg, Box, Piece...)
        public int Quantity { get; set; }       // Số lượng nhập
        public decimal UnitCost { get; set; }   // Giá nhập mỗi đơn vị
        public decimal TotalAmount { get; set; } // Tổng giá trị lô hàng (Quantity * UnitCost)
        public decimal SellingPrice { get; set; } // Tổng giá trị lô hàng (Quantity * UnitCost)
        public DateTime DateOfManufacture { get; set; }
        public DateTime ExpiryDate { get; set; } // Ngày hết hạn
        public string Status { get; set; }      // Trạng thái batch (Available, Pending, Expired...)
    }
}
