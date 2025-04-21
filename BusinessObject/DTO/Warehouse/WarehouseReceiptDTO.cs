using BusinessObject.DTO.Product;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.DTO.Warehouse
{
    public class WarehouseReceiptDTO
    {
        public long WarehouseReceiptId { get; set; }
        public string DocumentNumber { get; set; }
        public DateTime DocumentDate { get; set; }
        public long WarehouseId { get; set; }
        public string WarehouseName { get; set; }
        public string ImportType { get; set; }
        public string Supplier { get; set; }
        public DateTime DateImport { get; set; }
        public int TotalQuantity { get; set; }
        public decimal TotalPrice { get; set; }
        public List<BatchResponseDto> Batches { get; set; } // ✅ Trả về danh sách BatchDTO thay vì chuỗi JSON
        public bool IsApproved { get; set; }
    }
}
