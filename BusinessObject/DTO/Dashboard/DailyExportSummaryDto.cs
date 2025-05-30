using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.DTO.Dashboard
{
    public class DailyExportSummaryDto
    {
        public DateTime Date { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public int TotalExports { get; set; }
        public int TotalQuantity { get; set; }
        public decimal TotalAmount { get; set; }
    }
}
