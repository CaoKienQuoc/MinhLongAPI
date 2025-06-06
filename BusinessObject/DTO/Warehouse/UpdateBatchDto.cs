using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.ComponentModel.DataAnnotations;

namespace BusinessObject.DTO.Warehouse
{

    public class UpdateBatchDto
    {
        // Chuyển thành nullable để biết client có gửi hay không
        public int? ProductId { get; set; }
        public int? Quantity { get; set; }
        public decimal? ProfitMarginPercent { get; set; } // Phần trăm lợi nhuận
        public DateTime? DateOfManufacture { get; set; }
    }


}
