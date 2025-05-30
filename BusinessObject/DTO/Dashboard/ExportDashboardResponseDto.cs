using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.DTO.Dashboard
{
    public class ExportDashboardResponseDto
    {
        public List<DailyExportSummaryDto> DailySummaries { get; set; }
        public int TotalExports { get; set; }
        public int TotalQuantity { get; set; }
        public decimal TotalAmount { get; set; }
    }
}
