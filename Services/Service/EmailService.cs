using MailKit.Security;
using MimeKit.Text;
using MimeKit;
using Services.IService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using MailKit.Net.Smtp;
using BusinessObject.DTO.Email;
using static System.Net.WebRequestMethods;
using BusinessObject.Models;
using Repo.IRepository;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Services.Service
{

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly IUserRepository _userRepo;

        public EmailService(IConfiguration configuration, IUserRepository userRepository)
        {
            _configuration = configuration;
            _userRepo = userRepository;
        }

        public async Task<bool> SendEmailRegisterAccountAsync(string emailRequest, string subjectEmail, string fullName, string userNameUser, string passwordUser)
        {
            try
            {
                var emailBody = _configuration["EmailSetting:EmailRegisterAccount"];
                emailBody = emailBody.Replace("{PROJECT_NAME}", _configuration["EmailSetting:PROJECT_NAME"]);
                emailBody = emailBody.Replace("{FULL_NAME}", fullName);
                emailBody = emailBody.Replace("{USER_NAME}", userNameUser);
                emailBody = emailBody.Replace("{PASSWORD}", passwordUser);
                emailBody = emailBody.Replace("{PHONE_NUMBER}", _configuration["EmailSetting:PHONE_NUMBER"]);
                emailBody = emailBody.Replace("{EMAIL_ADDRESS}", _configuration["EmailSetting:EMAIL_ADDRESS"]);

                var emailHost = _configuration["EmailSetting:EmailHost"];
                var userName = _configuration["EmailSetting:EmailUsername"];
                var password = _configuration["EmailSetting:EmailPassword"];
                var email = new MimeMessage();
                email.From.Add(MailboxAddress.Parse(emailHost));
                email.To.Add(MailboxAddress.Parse(emailRequest));
                email.Subject = subjectEmail;
                email.Body = new TextPart(TextFormat.Html)
                {
                    Text = emailBody
                };
                using var smtp = new SmtpClient();
                await smtp.ConnectAsync(emailHost, 587, SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync(userName, password);
                await smtp.SendAsync(email);
                await smtp.DisconnectAsync(true);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<bool> SendEmailDebtReminderAsync(string emailRequest, string fullName, string orderId, DateTime dueDate)
        {
            try
            {
                // 🔄 Đọc và thay thế nội dung HTML từ appsettings
                var emailBody = _configuration["EmailSetting:EmailDebtReminder"];
                emailBody = emailBody.Replace("{PROJECT_NAME}", _configuration["EmailSetting:PROJECT_NAME"]);
                emailBody = emailBody.Replace("{FULL_NAME}", fullName);
                emailBody = emailBody.Replace("{ORDER_ID}", orderId);
                emailBody = emailBody.Replace("{DUE_DATE}", dueDate.ToString("dd/MM/yyyy"));
                emailBody = emailBody.Replace("{PHONE_NUMBER}", _configuration["EmailSetting:PHONE_NUMBER"]);
                emailBody = emailBody.Replace("{EMAIL_ADDRESS}", _configuration["EmailSetting:EMAIL_ADDRESS"]);

                // 🔐 Cấu hình thông tin email server
                var emailHost = _configuration["EmailSetting:EmailHost"];
                var userName = _configuration["EmailSetting:EmailUsername"];
                var password = _configuration["EmailSetting:EmailPassword"];

                // ✉️ Tạo nội dung email
                var email = new MimeMessage();
                email.From.Add(MailboxAddress.Parse(emailHost));
                email.To.Add(MailboxAddress.Parse(emailRequest));
                email.Subject = "Nhắc nhở thanh toán sắp đến hạn";
                email.Body = new TextPart(TextFormat.Html) { Text = emailBody };

                // 📤 Gửi mail thông qua SMTP
                using var smtp = new SmtpClient();
                await smtp.ConnectAsync(emailHost, 587, SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync(userName, password);
                await smtp.SendAsync(email);
                await smtp.DisconnectAsync(true);

                return true;
            }
            catch (Exception ex)
            {
                // ❌ Log lỗi nếu cần
                Console.WriteLine($"[Lỗi gửi mail nhắc nợ]: {ex.Message}");
                return false;
            }
        }

        private string GenerateOTP(int n)
        {
            string numbers = "1234567890";
            string otpKey = string.Empty;
            Random random = new Random();
            for (int i = 0; i < n; i++)
            {
                otpKey += numbers[random.Next(0, numbers.Length)];
            }
            return otpKey;
        }

        public async Task<bool> SendEmailAsync(SendOtpEmailRequest sendEmailRequest)
        {
            var otpKey = GenerateOTP(6);
            // Send OTP to Email       
            var emailBody = _configuration["EmailSetting:EmailBody"];
            emailBody = emailBody.Replace("{PROJECT_NAME}", _configuration["Project_MinhLong:PROJECT_NAME"]);
            emailBody = emailBody.Replace("{FULL_NAME}", sendEmailRequest.UserName);
            emailBody = emailBody.Replace("{EXPIRE_TIME}", "2");
            emailBody = emailBody.Replace("{OTP}", otpKey);
            emailBody = emailBody.Replace("{PHONE_NUMBER}", _configuration["Project_MinhLong:PHONE_NUMBER"]);
            emailBody = emailBody.Replace("{EMAIL_ADDRESS}", _configuration["Project_MinhLong:EMAIL_ADDRESS"]);
            var emailHost = _configuration["EmailSetting:EmailHost"];
            var userName = _configuration["EmailSetting:EmailUsername"];
            var password = _configuration["EmailSetting:EmailPassword"];
            var email = new MimeMessage();
            email.From.Add(MailboxAddress.Parse(emailHost));
            email.To.Add(MailboxAddress.Parse(sendEmailRequest.Email));
            email.Subject = _configuration.GetSection("EmailSetting")?["Subject"];
            email.Body = new TextPart(TextFormat.Html)
            {
                Text = emailBody
            };
            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(emailHost, 587, SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(userName, password);
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
            //InserDB
            OTPEmail otp = new OTPEmail()
            {
                Id = Guid.NewGuid(),
                Email = sendEmailRequest.Email,
                OtpKey = otpKey,
                CreatedTime = DateTime.Now,
                ExpireTime = 2,
            };
            otp.EndTime = otp.CreatedTime.GetValueOrDefault().AddMinutes(otp.ExpireTime ??= 2);
            var otpCheck = await _userRepo.AddOTPEmail(otp);
            if(otpCheck == null)
            {
                throw new Exception("Cannot insert otp to database");
            }
            return true;
        }

        public async Task<bool> CheckOtpEmail(CheckOtpRequest checkOtpRequest)
        {
            var user = await _userRepo.GetUserByEmailAsync(checkOtpRequest.Email);
            var otpEmail = await _userRepo.GetOTPEmailByEmail(checkOtpRequest.Email);
            if (otpEmail == null)
            {
                throw new Exception("OTP không đúng. Vui lòng nhập lại");
            }
            if (otpEmail.EndTime < DateTime.Now)
            {
                throw new Exception("OTP đã hết hạn");
            }
            if (!otpEmail.OtpKey.Equals(checkOtpRequest.OtpRequest))
            {
                return false;
            }
            user.VerifyEmail = true;
            await _userRepo.SaveAsync();
            return true;
        }

        public async Task<bool> SendDamagedStockNotificationEmailAsync(
        string toEmail,
        string warehouseName,
        decimal? totalAmount,
        IEnumerable<DamagedStock> items)
        {
            // 1. Lấy template từ appsettings
            var template = _configuration["EmailSetting:EmailDamagedStockTemplate"];

            // 2. Build danh sách item
            var itemListHtml = string.Join("",
                items.Select(i =>
                    $"<li>{i.Product.ProductName} – Số lượng: {i.Quantity} (Lô {i.BatchId})</li>"));

            // 3. Thay placeholder
            var body = template
                .Replace("{WAREHOUSE}", warehouseName)
                .Replace("{COUNT}", items.Count().ToString())
                .Replace("{ITEMS}", itemListHtml)
                .Replace("{TOTAL_AMOUNT}", totalAmount?.ToString("N0") ?? "0") // ➕ Thêm dòng này
                .Replace("{PROJECT_NAME}", _configuration["Project_MinhLong:PROJECT_NAME"])
                .Replace("{EMAIL_ADDRESS}", _configuration["Project_MinhLong:EMAIL_ADDRESS"])
                .Replace("{PHONE_NUMBER}", _configuration["Project_MinhLong:PHONE_NUMBER"]);

            // 4. Tạo email
            var emailHost = _configuration["EmailSetting:EmailHost"];
            var userName = _configuration["EmailSetting:EmailUsername"];
            var password = _configuration["EmailSetting:EmailPassword"];

            var email = new MimeMessage();
            email.From.Add(MailboxAddress.Parse(emailHost));
            email.To.Add(MailboxAddress.Parse(toEmail));
            email.Subject = "Thông báo: Hàng lỗi đã nhập kho huỷ";
            email.Body = new TextPart(TextFormat.Html) { Text = body };

            // 5. Gửi
            try
            {
                using var smtp = new SmtpClient();
                await smtp.ConnectAsync(emailHost, 587, SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync(userName, password);
                await smtp.SendAsync(email);
                await smtp.DisconnectAsync(true);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error Sending DamagedStock Email]: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendOrderCancelNotificationEmailAsync(
    string toEmail,
    string customerName,
    string orderCode,
    decimal? refundAmount)
        {
            // 1. Lấy template từ appsettings
            var template = _configuration["EmailSetting:EmailOrderCancelTemplate"];

            // 2. Build nội dung hoàn tiền
            string refundText = refundAmount.HasValue && refundAmount.Value > 0
                ? $"Số tiền <b>{refundAmount.Value:N0} VNĐ</b> sẽ được hoàn trả trong thời gian sớm nhất."
                : "Quý khách không phát sinh thanh toán, không cần hoàn tiền.";

            // 3. Thay placeholder
            var body = template
                .Replace("{CUSTOMER_NAME}", customerName)
                .Replace("{ORDER_CODE}", orderCode)
                .Replace("{REFUND_TEXT}", refundText)
                .Replace("{PROJECT_NAME}", _configuration["Project_MinhLong:PROJECT_NAME"])
                .Replace("{EMAIL_ADDRESS}", _configuration["Project_MinhLong:EMAIL_ADDRESS"])
                .Replace("{PHONE_NUMBER}", _configuration["Project_MinhLong:PHONE_NUMBER"]);

            // 4. Tạo email
            var emailHost = _configuration["EmailSetting:EmailHost"];
            var userName = _configuration["EmailSetting:EmailUsername"];
            var password = _configuration["EmailSetting:EmailPassword"];

            var email = new MimeMessage();
            email.From.Add(MailboxAddress.Parse(emailHost));
            email.To.Add(MailboxAddress.Parse(toEmail));
            email.Subject = "Thông báo huỷ đơn xuất kho & hoàn tiền";
            email.Body = new TextPart(TextFormat.Html) { Text = body };

            // 5. Gửi
            try
            {
                using var smtp = new SmtpClient();
                await smtp.ConnectAsync(emailHost, 587, SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync(userName, password);
                await smtp.SendAsync(email);
                await smtp.DisconnectAsync(true);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error Sending OrderCancel Email]: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendReturnOrderCancelNotificationEmailAsync(
    string toEmail,
    string customerName,
    string returnRequestCode)
        {
            // 1. Lấy template từ appsettings
            var template = _configuration["EmailSetting:EmailOrderCancelTemplate"];

            

            // 3. Thay placeholder
            var body = template
                .Replace("{CUSTOMER_NAME}", customerName)
                .Replace("{ORDER_CODE}", returnRequestCode)
                .Replace("{PROJECT_NAME}", _configuration["Project_MinhLong:PROJECT_NAME"])
                .Replace("{EMAIL_ADDRESS}", _configuration["Project_MinhLong:EMAIL_ADDRESS"])
                .Replace("{PHONE_NUMBER}", _configuration["Project_MinhLong:PHONE_NUMBER"]);

            // 4. Tạo email
            var emailHost = _configuration["EmailSetting:EmailHost"];
            var userName = _configuration["EmailSetting:EmailUsername"];
            var password = _configuration["EmailSetting:EmailPassword"];

            var email = new MimeMessage();
            email.From.Add(MailboxAddress.Parse(emailHost));
            email.To.Add(MailboxAddress.Parse(toEmail));
            email.Subject = "Thông báo huỷ đơn xuất kho & hoàn tiền";
            email.Body = new TextPart(TextFormat.Html) { Text = body };

            // 5. Gửi
            try
            {
                using var smtp = new SmtpClient();
                await smtp.ConnectAsync(emailHost, 587, SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync(userName, password);
                await smtp.SendAsync(email);
                await smtp.DisconnectAsync(true);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error Sending OrderCancel Email]: {ex.Message}");
                return false;
            }
        }
    }
}