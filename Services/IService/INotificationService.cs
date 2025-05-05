using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessObject.Models;

namespace Services.IService
{
    public interface INotificationService
    {
        Task<List<Notification>> GetNotificationsForUserAsync(Guid userId);
        Task<bool> MarkAsReadAsync(Guid notificationId, Guid currentUserId);

        Task<Notification> GetNotificationDetailAsync(Guid notificationId, Guid currentUserId);

    }

}
