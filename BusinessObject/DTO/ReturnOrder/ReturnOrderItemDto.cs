using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.DTO.ReturnOrder
{
    public class ReturnOrderItemDto
    {
        public long ProductId { get; set; }
        public string ProductName { get; set; }       // để front-end hiển thị
        public int OrderedQuantity { get; set; }      // số lượng gốc
        public int ReturnQuantity { get; set; }       // số lượng user nhập lại
    }
}
