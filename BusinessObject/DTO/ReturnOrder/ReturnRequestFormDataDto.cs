using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.DTO.ReturnOrder
{
    public class ReturnRequestFormDataDto
    {
        public Guid OrderId { get; set; }
        public string? Note { get; set; }
        public List<ReturnRequestDetailFormDataDto> Details { get; set; }
    }
}
