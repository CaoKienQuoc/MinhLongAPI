using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.DTO.Dashboard
{
    public class WarehouseDashboardRangeDto
    {
        public List<DailyWarehouseSummaryDto> DailySummaries { get; set; } = new();
        public int TotalReceipts { get; set; }
        public int TotalQuantity { get; set; }
        public decimal TotalPrice { get; set; }
    }
}
