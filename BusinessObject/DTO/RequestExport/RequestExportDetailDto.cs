using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.DTO.RequestExport
{
    public class RequestExportDetailDto
    {
        public int RequestExportDetailId { get; set; }
        public long ProductId { get; set; }
        public string ProductName { get; set; }      // ✅ Mới thêm
        public string Unit { get; set; }             // ✅ Nếu có
        public decimal? Price { get; set; }          // ✅ Nếu cần
        public int RequestedQuantity { get; set; }
        public decimal TotalAmount { get; set; }
    }


}
