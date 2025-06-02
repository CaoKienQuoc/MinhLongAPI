using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.DTO.Dashboard
{
    public class TopProductDto
    {
        public long ProductId { get; set; }
        public string ProductName { get; set; }
        public int QuantitySold { get; set; }
    }
}
