using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.DTO.Dashboard
{
    public class DailyReturnWarehouseReceiptSummaryDto
    {
        public DateTime Date { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public int TotalReturnReceipts { get; set; }
        public int TotalQuantity { get; set; }
    }
}
