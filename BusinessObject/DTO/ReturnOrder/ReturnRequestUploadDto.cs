using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace BusinessObject.DTO.ReturnOrder
{
    public class ReturnRequestUploadDto
    {
        public Guid OrderId { get; set; }

        // ✅ Danh sách sản phẩm (dùng custom binding)
        public List<FlattenedReturnItemDto> Items { get; set; }

        public List<IFormFile>? ProofImages { get; set; }
    }

}
