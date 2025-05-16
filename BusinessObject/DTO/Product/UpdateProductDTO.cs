using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.DTO.Product
{
    public class UpdateProductDTO
    {
        public string? ProductCode { get; set; }
        public string? ProductName { get; set; }
        public string? Unit { get; set; }
        public int? DefaultExpiration { get; set; }
        public long? CategoryId { get; set; }
        public string? Description { get; set; }
        public int? TaxId { get; set; }

        // ✅ Danh sách hình ảnh
        public List<IFormFile>? Images { get; set; }
    }
}
