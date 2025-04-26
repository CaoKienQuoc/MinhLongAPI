using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.DTO.ReturnOrder
{
    public class ReturnRequestProdductDetailDto
    {
        public Guid ReturnRequestDetailId { get; set; }
        public Guid OrderDetailId { get; set; }
        public long ProductId { get; set; }
        public string Reason { get; set; }
        public int QuantityReturned { get; set; }
        public List<ReturnRequestImageDto> Images { get; set; }
    }
}
