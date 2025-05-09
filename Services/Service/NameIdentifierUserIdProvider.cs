using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace Services.Service
{
    public class NameIdentifierUserIdProvider : IUserIdProvider
    {
        public string GetUserId(HubConnectionContext connection)
        {
            // ✅ Bắt đúng "UserId" từ token payload của bạn
            var userId = connection.User?.FindFirst("UserId")?.Value;

            Console.WriteLine($"🔗 GetUserId called, UserId: {userId}");
            return userId;
        }
    }
}
