using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.DTO
{
    public class AdminDashboardDto
    {
        public int TotalAccounts { get; set; }
        public int ActiveAccounts { get; set; }
        public int InactiveAccounts { get; set; }
        public int TotalAgencies { get; set; }
        public int TotalSalesManagers { get; set; }
        public int TotalWarehouseManagers { get; set; }
        public int TotalRegisterAccounts { get; set; }
        public int ApprovedRegisterAccounts { get; set; }    // Đã duyệt (AccountRegisterStatus == "Approved")
        public int PendingRegisterAccounts { get; set; }     // Chưa duyệt (AccountRegisterStatus == "Pending")
        public int CanceledRegisterAccounts { get; set; }    // Bị từ chối (AccountRegisterStatus == "Canceled")
        public int UnverifiedEmailCount { get; set; }
    }

}
