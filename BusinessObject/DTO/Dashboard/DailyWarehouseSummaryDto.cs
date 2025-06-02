using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.DTO.Dashboard
{
    public class DailyWarehouseSummaryDto
    {
        public DateTime Date { get; set; }
        public int Month { get; set; }     // Tháng của ngày
        public int Year { get; set; }      // Năm của ngày (nếu cần)
        public int TotalReceipts { get; set; }
        public int TotalQuantity { get; set; }
        public decimal TotalPrice { get; set; }
    }
}
