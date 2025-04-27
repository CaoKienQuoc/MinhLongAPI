using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.DTO.ReturnOrder
{
    public class ReturnRequestProdductDto
    {
        public Guid ReturnRequestId { get; set; }
        public Guid OrderId { get; set; }
        public DateTime CreatedAt { get; set; }
        
        public string CreatedByUserName { get; set; }
        public string Status { get; set; }
        public string? Note { get; set; }
        public List<ReturnRequestProdductDetailDto> Details { get; set; }
    }
}
