using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.DTO.Warehouse
{
    public class EmployeeDto
    {
        public Guid UserId { get; set; }     // Thêm UserId
        public string FullName { get; set; }
    }
}
