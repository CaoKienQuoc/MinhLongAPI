using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.DTO
{
    // DTOs
    public class CreateRoomDto
    {
        public Guid? RoomName { get; set; } = Guid.NewGuid();
        public List<Guid> MemberIds { get; set; }
    }
}
