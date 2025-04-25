using Azure.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.Models
{
    public class Order
    {
        [Key]
        public Guid OrderId { get; set; }
        public string OrderCode { get; set; }
        public DateTime OrderDate { get; set; }
        /*public long SalesAgentId { get; set; }*/
        public decimal Discount { get; set; }
        public decimal FinalPrice { get; set; }
        public string Status { get; set; } // PENDING, PROCESSING, CANCELED
        // Thêm khóa ngoại để liên kết với RequestProduct
        public Guid RequestId { get; set; }

        [ForeignKey("RequestId")]
        public RequestProduct RequestProduct { get; set; } // Navigation property

        // Navigation property
        public virtual List<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
        public ICollection<PaymentHistory> PaymentHistories { get; set; }
        /*public ICollection<RequestExport> RequestExports { get; set; }*/
        // ✅ Quan hệ 1-1: Một Order chỉ có một RequestExport
        public virtual RequestExport RequestExport { get; set; }

        // 1-n: Một Order có thể có nhiều bản ghi tạm TemporaryStockExport
        public virtual ICollection<TemporaryStockExport> TemporaryStockExports { get; set; } = new List<TemporaryStockExport>();

    }
}
