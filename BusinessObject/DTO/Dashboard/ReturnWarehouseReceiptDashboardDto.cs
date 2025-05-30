using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.DTO.Dashboard
{
    public class ReturnWarehouseReceiptDashboardDto
    {
        public List<DailyReturnWarehouseReceiptSummaryDto> DailySummaries { get; set; }
        public int TotalReturnReceipts { get; set; }
        public int TotalQuantity { get; set; }
    }
}
