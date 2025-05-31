using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.Models
{
    public class ExportTransaction
    {
        [Key]
        public long ExportTransactionId { get; set; }
        public string DocumentNumber { get; set; }
        public DateTime DocumentDate { get; set; }
        public long WarehouseId { get; set; }
        public string ExportType { get; set; } // Sales, Disposal, Transfer
        public DateTime ExportDate { get; set; }
        public string Note { get; set; }

        [ForeignKey("RequestExport")]
        public int RequestExportId { get; set; }
        public RequestExport RequestExport { get; set; }

        public string? AgencyName { get; set; }

        public string? OrderCode { get; set; }

        [ForeignKey("WarehouseId")]
        public Warehouse Warehouse { get; set; }

        [ForeignKey("WarehouseTransferRequest")]
        public long? WarehouseTransferRequestId { get; set; }

        public string? Reason { get; set; } // Lý do hủy đơn hàng
        public WarehouseTransferRequest? WarehouseTransferRequest { get; set; }
        public ICollection<ExportTransactionDetail> ExportTransactionDetail { get; set; }
    }
}
