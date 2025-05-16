using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace BusinessObject.DTO.ReturnOrder
{
    public class ReturnRequestGroupedDto
    {
        public Guid OrderId { get; set; }
        public List<ReturnProductItemDto> Items { get; set; } = new();

        // ✅ Ảnh xác minh cho toàn bộ đơn hàng
        public List<IFormFile>? ProofImages { get; set; }
    }

}
