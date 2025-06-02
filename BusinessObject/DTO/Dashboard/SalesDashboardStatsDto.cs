using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.DTO.Dashboard
{
    public class SalesDashboardStatsDto
    {
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalProfit { get; set; }
        public decimal ProfitMarginPercent { get; set; }
        public List<TopProductDto> TopProducts { get; set; }
        public List<DailySalesStatDto> DailyStats { get; set; }
    }
}
