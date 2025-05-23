using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.DTO.Warehouse
{
    public class ExportWarehouseReceiptDTO
    {
        public long ExportWarehouseReceiptId { get; set; }
        public string DocumentNumber { get; set; }
        public DateTime DocumentDate { get; set; }
        public DateTime ExportDate { get; set; }
        public string ExportType { get; set; }
        public int TotalQuantity { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; }
        public long WarehouseId { get; set; }

        public string WarehouseName { get; set; }
        public int RequestExportId { get; set; }

        public string? OrderCode { get; set; }
        public string? AgencyName { get; set; }
        public List<ExportWarehouseReceiptDetailDTO> Details { get; set; }
    }

    public class ExportWarehouseReceiptDetailDTO
    {
        public long WarehouseProductId { get; set; }
        public long ProductId { get; set; }
        public string ProductName { get; set; }
        public string BatchNumber { get; set; }
        public int RequestedQuantity { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalProductAmount { get; set; }
        public DateTime ExpiryDate { get; set; }
    }
}
