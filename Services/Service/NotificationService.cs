using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessObject.Models;
using Repo.IRepository;
using Services.IService;

namespace Services.Service
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _notificationRepository;

        public NotificationService(INotificationRepository notificationRepository)
        {
            _notificationRepository = notificationRepository;
        }

        public async Task<List<Notification>> GetNotificationsForUserAsync(Guid userId)
        {
            return await _notificationRepository.GetNotificationsByUserIdAsync(userId);
        }
    }

}
