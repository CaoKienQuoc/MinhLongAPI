using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.DTO.Dashboard
{
    public class ProfitByYearDto
    {
        public int Year { get; set; }
        public decimal TotalImportCost { get; set; }
        public decimal TotalExportRevenue { get; set; }
        public decimal ProfitAmount { get; set; }
        public decimal ProfitPercentage { get; set; }
    }

}
