using BusinessObject.DTO.Product;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace BusinessObject.DTO.Warehouse
{
    public class WarehouseReceiptRequest
    {
        [Required]
        public string DocumentNumber { get; set; } // Số chứng từ

        [JsonIgnore]
        public DateTime DocumentDate { get; set; } // Ngày chứng từ

        [Required]
        public long WarehouseId { get; set; } // Kho nhập

        [Required]
        public string ImportType { get; set; } // Loại nhập (Nhập sản xuất, Nhập chuyển, Nhập trả)

        [Required]
        public string Supplier { get; set; } // Nhà cung cấp

        [JsonIgnore]
        public DateTime DateImport { get; set; } // Ngày nhập

        public string Note { get; set; } = "nothing";

        // Không nhập từ người dùng, sẽ tính toán tự động
        [JsonIgnore]
        public int TotalQuantity { get; set; }
        [JsonIgnore]
        public decimal TotalPrice { get; set; }

        // Danh sách chi tiết nhập hàng
        [Required]
        public List<BatchRequest> Batches { get; set; } = new List<BatchRequest>();

        public bool IsApproved { get; set; }

        public Guid? OrderId { get; set; } // Dành cho ImportCoordination

    }

}
