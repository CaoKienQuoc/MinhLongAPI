using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace BusinessObject.DTO.Product
{
    public class BatchRequest
    {

        [Required]
        public long ProductId { get; set; } // Mã sản phẩm

        [Required]
        public string Unit { get; set; } // Đơn vị tính

        [Required]
        public int Quantity { get; set; } // Số lượng

        [Required]
        public decimal UnitCost { get; set; } // Đơn giá

        [JsonIgnore]
        public decimal TotalAmount { get; set; } // Thành tiền

        [Required]
        public DateTime DateOfManufacture { get; set; } // ✅ Ngày sản xuất (Bắt buộc nhập)

    }

}
