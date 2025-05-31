using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.DTO.RequestExport
{
    public class CancelRequestExportModel
    {
        public int RequestExportId { get; set; }
        public string Reason { get; set; }
    }

}
