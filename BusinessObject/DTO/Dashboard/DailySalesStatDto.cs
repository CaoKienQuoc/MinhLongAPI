using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.DTO.Dashboard
{
    public class DailySalesStatDto
    {
        public DateTime Date { get; set; }
        public int Day => Date.Day;
        public int Month => Date.Month;
        public int Year => Date.Year;

        public int OrderCount { get; set; }
        public decimal Revenue { get; set; }
        public decimal Profit { get; set; }
        public decimal ProfitMarginPercent { get; set; }
    }

}
