using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace BusinessObject.DTO.ReturnOrder
{
    public class ReturnProductItemDto
    {
        public Guid OrderId { get; set; }
        public Guid OrderDetailId { get; set; }
        public int Quantity { get; set; }
        public string Reason { get; set; }

        // ✅ Chuẩn nhất: List<IFormFile>
        public List<IFormFile>? ItemImages { get; set; }
    }


}
