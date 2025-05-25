using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.DTO.Order
{
    public class OrderDto
    {
        public Guid OrderId { get; set; }
        public string OrderCode { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal Discount { get; set; }
        public decimal FinalPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public string Status { get; set; }
        public long AgencyId { get; set; }
        public string SalesName { get; set; } // 👈 Lấy từ ApprovedByEmployee
        public string AgencyName { get; set; }
        public string RequestCode { get; set; }

        public List<OrderDetailDto> OrderDetails { get; set; }
    }

}
