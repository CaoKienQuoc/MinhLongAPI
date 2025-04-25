using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.DTO.ReturnOrder
{
    public class ReturnRequestDetailDto
    {
        public Guid OrderDetailId { get; set; }
        public long ProductId { get; set; }
        public int Quantity { get; set; }
        public string Reason { get; set; }
        public List<string> ImageUrls { get; set; }
    }
}
