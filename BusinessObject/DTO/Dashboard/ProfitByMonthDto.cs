using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.DTO.Dashboard
{
    public class ProfitByMonthDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public decimal TotalImportCost { get; set; }   // Tổng tiền nhập (giá vốn)
        public decimal TotalExportRevenue { get; set; } // Tổng tiền xuất (doanh thu)
        public decimal ProfitAmount { get; set; }      // Lợi nhuận = doanh thu - giá vốn
        public decimal ProfitPercentage { get; set; }  // % lãi = lợi nhuận / giá vốn * 100 (nếu giá vốn > 0)
    }
}
