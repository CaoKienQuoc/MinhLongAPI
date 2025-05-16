using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.Models
{
    public class ReturnRequestDetail
    {
        [Key]
        public Guid ReturnRequestDetailId { get; set; }

        public Guid ReturnRequestId { get; set; }
        [ForeignKey(nameof(ReturnRequestId))]
        public ReturnRequest ReturnRequest { get; set; }

        public Guid OrderDetailId { get; set; }
        [ForeignKey(nameof(OrderDetailId))]
        public OrderDetail OrderDetail { get; set; }

        public int QuantityReturned { get; set; }

        public string Reason { get; set; }


        public long ProductId { get; set; } // optional nếu cần thêm tra cứu nhanh

        [ForeignKey(nameof(ProductId))]
        public Product Product { get; set; }
    }

}
