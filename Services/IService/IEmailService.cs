using BusinessObject.DTO.Email;
using BusinessObject.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.IService
{
    public interface IEmailService
    {
        Task<bool> SendEmailRegisterAccountAsync(string emailRequest, string subjectEmail, string fullName, string userNameUser, string passwordUser);
        Task<bool> CheckOtpEmail(CheckOtpRequest checkOtpRequest);
        Task<bool> SendEmailAsync(SendOtpEmailRequest sendEmailRequest);
        Task<bool> SendEmailDebtReminderAsync(string emailRequest, string fullName, string orderId, DateTime dueDate);
        Task<bool> SendDamagedStockNotificationEmailAsync(string toEmail,string warehouseName, decimal? totalAmount, IEnumerable<DamagedStock> items);
        Task<bool> SendOrderCancelNotificationEmailAsync(string toEmail,string customerName,string orderCode,decimal? refundAmount);
        Task<bool> SendReturnOrderCancelNotificationEmailAsync(string toEmail, string customerName, string returnRequestCode, string reason);

    }
}