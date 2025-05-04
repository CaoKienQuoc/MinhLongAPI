using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessObject.Models;

namespace Repo.IRepository
{
    public interface INotificationRepository
    {
        Task AddAsync(Notification notification);
        Task SaveChangesAsync();
        Task<List<Notification>> GetNotificationsByUserIdAsync(Guid userId);
        Task<Notification> GetByIdAsync(Guid id);
    }

}
