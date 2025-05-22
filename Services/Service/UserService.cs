using BusinessObject.DTO;
using BusinessObject.DTO.Email;
using BusinessObject.DTO.Warehouse;
using BusinessObject.Models;
using DataAccessLayer;
using MailKit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Repo.IRepository;
using Repo.Repository;
using Services.IService;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Services.Service
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly JwtService _jwtService;
        private readonly IEmailService _mailService;
        private readonly IAgencyAccountRepository _agencyAccountRepository;
        private readonly IAgencyAccountLevelRepository _agencyAccountLevelRepository;
        private readonly IAgencyLevelRepository _agencyLevelRepository;
        private readonly IContractService _contractService;
        private readonly IContractRepository _contractRepository;
        private readonly IHubContext<NotificationHub> _hub;
        private readonly INotificationRepository _notificationRepository;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IRefreshTokenRepository _refreshTokenRepo;
        private readonly ILogger<UserService> _logger;

        public UserService(IUserRepository userRepository, JwtService jwtService, IEmailService mailService, 
            IAgencyAccountRepository agencyAccountRepository, IAgencyAccountLevelRepository agencyAccountLevelRepository, 
            IAgencyLevelRepository agencyLevelRepository, IContractService contractService, IContractRepository contractRepository,
            IHubContext<NotificationHub> hub, INotificationRepository notificationRepository, IConfiguration configuration, IHttpContextAccessor httpContextAccessor,IRefreshTokenRepository refreshTokenRepository, ILogger<UserService> logger)
        {
            _userRepository = userRepository;
            _jwtService = jwtService;
            _mailService = mailService;
            _agencyAccountRepository = agencyAccountRepository;
            _agencyAccountLevelRepository = agencyAccountLevelRepository;
            _agencyLevelRepository = agencyLevelRepository;
            _contractService = contractService;
            _contractRepository = contractRepository;
            _hub = hub;
            _notificationRepository = notificationRepository;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
            _refreshTokenRepo = refreshTokenRepository;
            _logger = logger;
        }


        public async Task<PagedResult<UserDto>> GetUsersAsync()
        {
            int totalItems = await _userRepository.GetTotalUsersAsync(); // Tổng số user
            var allUsers = await _userRepository.GetUsersWithAgencyDetailsAsync(); // Lấy toàn bộ user

            // Loại bỏ admin trước khi phân trang
            var filteredUsers = allUsers
                .Where(user => !user.Username.Equals("admin", StringComparison.OrdinalIgnoreCase))
                .ToList();

            int totalFilteredItems = filteredUsers.Count; // Tổng số user sau khi lọc admin


            // ✅ Chuyển đổi sang DTO
            List<UserDto> userDtos = filteredUsers.Select(u => new UserDto
            {
                UserId = u.UserId,
                UserName = u.Username,
                Email = u.Email,
                Phone = u.Phone,
                Status = u.Status,
                VerifyEmail = u.VerifyEmail,
                UserType = u.UserType,

                FullName = u.Employee?.FullName,
                Position = u.Employee?.Position,
                Department = u.Employee?.Department,
                AgencyName = u.AgencyAccount?.AgencyName,

                // ✅ Lấy địa chỉ tùy theo loại người dùng
                Address = u.Employee != null ?
            $"{u.Employee.Address?.Street}, {u.Employee.Address?.Ward?.WardName}, {u.Employee.Address?.District?.DistrictName}, {u.Employee.Address?.Province?.ProvinceName}"
            :
            $"{u.AgencyAccount?.Address?.Street}, {u.AgencyAccount?.Address?.Ward?.WardName}, {u.AgencyAccount?.Address?.District?.DistrictName}, {u.AgencyAccount?.Address?.Province?.ProvinceName}",

                Contracts = u.AgencyAccount?.Contracts.Select(c => new ContractDto
                {
                    ContractId = c.ContractId,
                    FileName = c.FileName,
                    FilePath = c.FilePath,
                    FileType = c.FileType,
                    CreatedAt = c.CreatedAt
                }).ToList() ?? new List<ContractDto>()
            }).ToList();

            return new PagedResult<UserDto>
            {
                Items = userDtos,
                TotalItems = totalFilteredItems, // ✅ Cập nhật lại số lượng sau khi lọc
            };
        }

        public async Task<User> GetUserByIdAsync(Guid userId)
        {
            var user = await _userRepository.GetUserByIdAsync(userId);
            return user;
        }

        public async Task<User> GetEmployeeByIdAsync(Guid userId)
        {
            var user = await _userRepository.GetEmployeeByIdAsync(userId);
            return user;
        }

        public async Task<UserDetailDto> GetAgencyUserByIdAsync(Guid userId)
        {
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null)
                throw new Exception("Không tìm thấy User!");

            var dto = new UserDetailDto
            {
                UserId = user.UserId,
                Username = user.Username,
                UserType = user.UserType,
                Email = user.Email,
                Phone = user.Phone,
                Status = user.Status,
                VerifyEmail = user.VerifyEmail,
                AgencyLevelName = null,
                AgencyScore = 0,
                CreditLimit = null,
                Position = null,
                Department = null,
                Contracts = new List<ContractDto>() // Luôn khởi tạo
            };

            if (user.UserType?.ToUpper() == "AGENCY")
            {
                var agency = await _userRepository.GetAgencyAccountByUserIdAsync(userId);
                if (agency != null)
                {

                    dto.AgencyScore = agency.AgencyScore;
                    // Nếu đại lý có cấp độ
                    var level = await _agencyAccountLevelRepository.GetLatestLevelByAgencyIdAsync(agency.AgencyId);
                    if (level != null)
                    {
                        dto.AgencyLevelName = level.Level?.LevelName;
                        dto.CreditLimit = level.Level?.CreditLimit;
                    }

                    // ✅ Lấy danh sách hợp đồng
                    if (agency.Contracts != null && agency.Contracts.Any())
                    {
                        dto.Contracts = agency.Contracts.Select(c => new ContractDto
                        {
                            ContractId = c.ContractId,
                            FileName = c.FileName,
                            FilePath = c.FilePath,
                            FileType = c.FileType,
                            CreatedAt = c.CreatedAt
                        }).ToList();
                    }

                   
                }
            }
            else if (user.UserType?.ToUpper() == "EMPLOYEE")
            {
                var employee = await _userRepository.GetByEmployeeUserIdAsync(userId);
                if (employee != null)
                {
                    dto.Position = employee.Position;
                    dto.Department = employee.Department;
                }
            }

            return dto;
        }



        // Hàm kiểm tra User có RoleId = 1 không
        private bool UserHasRole(Guid userId, int roleId)
        {
            return _userRepository.GetUserRoles(userId).Any(r => r.RoleId == roleId);
        }


        public async Task<RegisterAccount> RegisterUserRequestAsync(RegisterRequest request)
        {
            // ✅ Kiểm tra Email hợp lệ (chỉ khi có dữ liệu)
            if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains("@"))
            {
                throw new ArgumentException("Email không hợp lệ! Phải chứa ký tự '@'!");
            }
            if (string.IsNullOrWhiteSpace(request.Password))
            {
                throw new ArgumentException("Mật khẩu không được để trống!");
            }

            // ✅ Kiểm tra độ dài tối thiểu 8 ký tự
            if (request.Password.Length < 8)
            {
                throw new ArgumentException("Mật khẩu phải có ít nhất 8 ký tự!");
            }

            // ✅ Kiểm tra có ít nhất một chữ cái (a-z hoặc A-Z)
            if (!request.Password.Any(char.IsLetter))
            {
                throw new ArgumentException("Mật khẩu phải chứa ít nhất một chữ cái (a-z, A-Z)!");
            }

            // ✅ Kiểm tra có ít nhất một ký tự đặc biệt
            if (!Regex.IsMatch(request.Password, @"[\W_]"))  // `\W` đại diện cho ký tự không phải chữ cái hoặc số
            {
                throw new ArgumentException("Mật khẩu phải chứa ít nhất một ký tự đặc biệt (@, #, $, v.v.)!");
            }


            // ✅ Kiểm tra số điện thoại hợp lệ (chỉ khi có dữ liệu)
            if (string.IsNullOrWhiteSpace(request.Phone) || !Regex.IsMatch(request.Phone, @"^0\d{9}$"))
            {
                throw new ArgumentException("Số điện thoại không hợp lệ! Phải có 10 chữ số và bắt đầu bằng '0'.");
            }

            // ✅ Kiểm tra UserType hợp lệ
            if (string.IsNullOrWhiteSpace(request.UserType) ||
                (request.UserType.ToUpper() != "EMPLOYEE" && request.UserType.ToUpper() != "AGENCY"))
            {
                throw new ArgumentException("UserType phải là 'EMPLOYEE' hoặc 'AGENCY'!");
            }

            if (request.Username.Equals("admin"))
            {
                throw new ArgumentException("Không thể đặt tên người dùng là admin");
            }



            // ✅ Nếu UserType là EMPLOYEE -> Bắt buộc nhập FullName, Position, Department
            if (request.UserType.ToUpper() == "EMPLOYEE")
            {
                if (string.IsNullOrWhiteSpace(request.FullName))
                {
                    throw new ArgumentException("FullName là cần thiết đối với EMPLOYEE.");
                }

                // Regex pattern: Bắt đầu bằng chữ cái in hoa, chỉ chứa chữ cái và khoảng trắng
                string namePattern = @"^[\p{Lu}][\p{L}\s]*$";
                if (!Regex.IsMatch(request.FullName, namePattern))
                {
                    throw new ArgumentException("FullName phải bắt đầu bằng chữ cái viết hoa và chỉ chứa chữ cái và khoảng trắng.");
                }

                if (string.IsNullOrWhiteSpace(request.Position) ||
                    (request.Position.ToUpper() != "STAFF" && request.Position.ToUpper() != "MANAGER"))
                {
                    throw new ArgumentException("Position phải là 'STAFF' hoặc 'MANAGER' đối với EMPLOYEE.");
                }
                if (string.IsNullOrWhiteSpace(request.Department) ||
                    (request.Department.ToUpper() != "WAREHOUSE MANAGER" && request.Department.ToUpper() != "SALES MANAGER" && request.Department.ToUpper() != "ACCOUNTANT MANAGER" && request.Department.ToUpper() != "WAREHOUSE PLANNER"))
                {
                    throw new ArgumentException("Department phải là 'WAREHOUSE MANAGER' hoặc 'SALES MANAGER' hoặc 'ACCOUNTANT MANAGER' hoặc 'WAREHOUSE PLANNER' đối với EMPLOYEE.");
                }

                // ✅ Nếu là Employee thì AgencyName có thể null
                request.AgencyName = "unknow";

            }

            // ✅ Nếu UserType là AGENCY -> Bắt buộc nhập AgencyName, các trường khác có thể null
            if (request.UserType.ToUpper() == "AGENCY")
            {

                if (string.IsNullOrWhiteSpace(request.AgencyName))
                {
                    throw new ArgumentException("AgencyName là cần thiết đối với AGENCY.");
                }

                // ✅ Nếu là Agency thì FullName, Position, Department có thể null
                request.FullName = "unknow";
                request.Position = "unknow";
                request.Department = "unknow";
            }

            // ✅ Chuẩn hóa dữ liệu (chỉ khi có giá trị)
            request.FullName = !string.IsNullOrWhiteSpace(request.FullName)
                ? CultureInfo.CurrentCulture.TextInfo.ToTitleCase(request.FullName.Trim().ToLower())
                : null;

            request.Position = !string.IsNullOrWhiteSpace(request.Position)
                ? request.Position.Trim().ToUpper()
                : null;

            request.Department = !string.IsNullOrWhiteSpace(request.Department)
                ? request.Department.Trim().ToUpper()
                : null;

            request.UserType = request.UserType.Trim().ToUpper();
            request.Street = request.Street?.Trim();
            request.WardName = request.WardName?.Trim();
            request.DistrictName = request.DistrictName?.Trim();
            request.ProvinceName = request.ProvinceName?.Trim();
            request.AgencyName = request.AgencyName?.Trim();
            request.Password = request.Password?.Trim();

            // ✅ Bước 1: Tạo RegisterAccount
            var registerAccount = new RegisterAccount
            {
                Username = request.Username,
                Email = request.Email,
                Password = request.Password,
                Phone = request.Phone,
                UserType = request.UserType,
                FullName = request.FullName,
                Position = request.Position,
                Department = request.Department,
                Street = request.Street,
                WardName = request.WardName,
                DistrictName = request.DistrictName,
                ProvinceName = request.ProvinceName,
                AgencyName = request.AgencyName
            };

            var createdRegister = await _userRepository.RegisterUserRequestAsync(registerAccount);

            // ✅ Bước 2: Nếu là AGENCY và có ContractFiles => upload và lưu
            if (request.UserType == "AGENCY")
            {
                if (request.ContractFiles != null && request.ContractFiles.Any())
                {
                    var uploadedContracts = await _contractService.UploadContractsAsync(request.ContractFiles); // không cần AgencyId

                    var registerContracts = uploadedContracts.Select(c => new RegisterAccountContract
                    {
                        RegisterId = createdRegister.RegisterId,
                        FileName = c.FileName,
                        FilePath = c.FilePath,
                        FileType = c.FileType
                    }).ToList();

                    await _contractRepository.AddRangeRegisterContractsAsync(registerContracts);

                    createdRegister.Contracts = registerContracts;
                }
                else
                {
                    createdRegister.Contracts = new List<RegisterAccountContract>();
                }
            }
            else
            {
                createdRegister.Contracts = null;
            }

            return createdRegister;
        }
        /*// ✅ Duyệt tài khoản và chuyển dữ liệu từ RegisterAccount vào User
        public async Task<bool> ApproveUserAsync(int registerId)
        {
            RegisterAccount registerUser = await _userRepository.GetRegisterAccountByIdAsync(registerId);
            if(registerUser.UserType == "AGENCY")
            {
                _mailService.SendEmailRegisterAccountAsync(registerUser.Email, "Active Account SuccessFully!", registerUser.AgencyName, registerUser.Username, registerUser.Password);
            }
            else
            {
                _mailService.SendEmailRegisterAccountAsync(registerUser.Email, "Active Account SuccessFully!", registerUser.FullName, registerUser.Username, registerUser.Password);
            }

            // ✅ Gọi Repo để duyệt tài khoản
            return await _userRepository.ApproveUserAsync(registerId);
        }*/

        public async Task<bool> ApproveUserAsync(int registerId)
        {
            var registerUser = await _userRepository.GetRegisterAccountByIdAsync(registerId);
            if (registerUser == null)
                throw new KeyNotFoundException($"RegisterAccount với ID {registerId} không tìm thấy.");

            var approved = await _userRepository.ApproveUserAsync(registerId);
            if (!approved)
                throw new Exception("Không thể phê duyệt người dùng.");

            // Gọi lại để lấy thông tin đã cập nhật
            registerUser = await _userRepository.GetRegisterAccountByIdAsync(registerId);

            if (registerUser.UserType?.ToUpper() == "AGENCY")
            {
                var agencyAccount = await _agencyAccountRepository.GetByUsernameAsync(registerUser.Username);
                if (agencyAccount == null)
                    throw new Exception($"AgencyAccount không tìm thấy với Username: {registerUser.Username}");

                // 💡 Tìm nhân viên SALES MANAGER có thể quản lý
                var salesManagers = await _userRepository.GetEmployeesByRoleAsync("SALES MANAGER");

                Employee assignedManager = null;

                foreach (var manager in salesManagers)
                {
                    var count = await _agencyAccountRepository.CountManagedAgenciesAsync(manager.EmployeeId);
                    if (count < 20)
                    {
                        assignedManager = manager;
                        break;
                    }
                }

                if (assignedManager == null)
                    throw new Exception("Không có SALES MANAGER hiện tại có thể quản lý Đại lý");

                agencyAccount.ManagedByEmployeeId = assignedManager.EmployeeId;
                await _agencyAccountRepository.UpdateAsync(agencyAccount);


                /*// Gán level mặc định
                var defaultLevel = await _agencyLevelRepository.GetByIdAsync(3);
                if (defaultLevel == null)
                    throw new Exception("Cấp đại lý 3 (LevelId = 3) không tìm thấy.");

                var agencyAccountLevel = new AgencyAccountLevel
                {
                    AgencyId = agencyAccount.AgencyId,
                    LevelId = 3,
                    TotalDebtValue = 0,
                    OrderDiscount = defaultLevel.DiscountPercentage ?? 0,
                    MonthlyRevenue = 0,
                    OrderRevenue = 0,
                    ChangeDate = DateTime.Now
                };*/

                var defaultLevel = await _agencyLevelRepository.GetLowestLevelAsync();
                if (defaultLevel == null)
                    throw new Exception("Không tìm thấy cấp đại lý thấp nhất.");

                var agencyAccountLevel = new AgencyAccountLevel
                {
                    AgencyId = agencyAccount.AgencyId,
                    LevelId = defaultLevel.LevelId,
                    TotalDebtValue = 0,
                    OrderDiscount = defaultLevel.DiscountPercentage ?? 0,
                    MonthlyRevenue = 0,
                    OrderRevenue = 0,
                    ChangeDate = DateTime.Now
                };


                await _agencyAccountLevelRepository.AddAsync(agencyAccountLevel);
            }

            // Gửi email xác nhận
            switch (registerUser.UserType?.ToUpper())
            {
                case "AGENCY":
                    await _mailService.SendEmailRegisterAccountAsync(
                        registerUser.Email,
                        "Active Account Successfully!",
                        registerUser.AgencyName,
                        registerUser.Username,
                        registerUser.Password);
                    break;

                case "EMPLOYEE":
                case "ACCOUNTANT":
                    await _mailService.SendEmailRegisterAccountAsync(
                        registerUser.Email,
                        "Active Account Successfully!",
                        registerUser.FullName,
                        registerUser.Username,
                        registerUser.Password);
                    break;

                default:
                    throw new InvalidOperationException($"Không hỗ trợ UserType: {registerUser.UserType}");
            }

            return true;
        }


        //Logout
        /*public async Task<bool> LogoutAsync(string email)
        {
            var user = await _userRepository.GetUserByEmailAsync(email);
            if (user == null)
            {
                throw new ArgumentException("User không tìm thấy.");
            }

            // ✅ Nếu bạn dùng Session hoặc Refresh Token, xóa token tại đây
            // Ví dụ: user.RefreshToken = null;
            await _userRepository.UpdateUserAsync(user);

            return true; // Trả về true nếu logout thành công
        }*/

        public async Task<bool> LogoutAsync()
        {
            var refreshToken = _httpContextAccessor.HttpContext?.Request.Cookies["refresh_token"];

            if (!string.IsNullOrEmpty(refreshToken))
            {
                await _refreshTokenRepo.RevokeTokenAsync(refreshToken);
            }

            _httpContextAccessor.HttpContext.Response.Cookies.Delete("access_token");
            _httpContextAccessor.HttpContext.Response.Cookies.Delete("refresh_token");

            return true;
        }


        public async Task<(bool Success, string Message)> RefreshTokenAsync()
        {
            var refreshToken = _httpContextAccessor.HttpContext?.Request.Cookies["refresh_token"];
            if (string.IsNullOrEmpty(refreshToken))
                return (false, "Không tìm thấy refresh token.");

            var tokenInDb = await _refreshTokenRepo.GetByTokenAsync(refreshToken);

            if (tokenInDb == null || tokenInDb.ExpiredAt < DateTime.UtcNow || tokenInDb.IsRevoked)
                return (false, "Refresh token không hợp lệ hoặc đã hết hạn.");

            var user = await _userRepository.GetByIdAsync(tokenInDb.UserId);
            if (user == null)
                return (false, "Người dùng không tồn tại.");

            var userRole = await _userRepository.GetUserRoleByUserIdAsync(user.UserId);
            long roleId = userRole?.RoleId ?? 0;

            var newAccessToken = await _jwtService.GenerateJwtTokenAsync(user, roleId);
            var accessTokenExpires = GetVnNow().AddMinutes(double.Parse(_configuration["Jwt:ExpireMinutes"]));

            _httpContextAccessor.HttpContext.Response.Cookies.Append("access_token", newAccessToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = accessTokenExpires
            });

            _logger.LogInformation($"[RefreshToken] User {user.Username} làm mới access token lúc {GetVnNow()}");

            return (true, "Token đã được làm mới thành công.");
        }



        public async Task<bool> UpdateUserAccountAsync(Guid userId, UpdateUserRequest request)
        {
            // ✅ 1. Lấy thông tin User từ database
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null)
            {
                throw new ArgumentException("User không tìm thấy.");
            }

            // ✅ 2. Cập nhật thông tin User (Email, Phone)
            if (!string.IsNullOrWhiteSpace(request.Email))
                user.Email = request.Email.Trim();

            if (!string.IsNullOrWhiteSpace(request.Phone))
                user.Phone = request.Phone.Trim();

            if (!string.IsNullOrWhiteSpace(request.Username))
                user.Username = request.Username.Trim();

            if (!string.IsNullOrWhiteSpace(request.Password))
            {
                // ✅ Kiểm tra độ dài phải đúng 9 ký tự
                if (request.Password.Length != 8)
                {
                    throw new ArgumentException("Mật khẩu phải có ít nhất 8 ký tự!");
                }

                // ✅ Kiểm tra có ít nhất một ký tự đặc biệt
                if (!Regex.IsMatch(request.Password, @"[!@#$%^&*]"))
                {
                    throw new ArgumentException("Mật khẩu phải chứa ít nhất một ký tự đặc biệt (@, #, $, v.v.)!");
                }

                // ✅ Kiểm tra có ít nhất một chữ cái viết hoa
                if (!Regex.IsMatch(request.Password, @"[A-Z]"))
                {
                    throw new ArgumentException("Mật khẩu phải chứa ít nhất một chữ cái (a-z, A-Z)!");
                }

                // ✅ Hash mật khẩu trước khi lưu
                user.Password = BCrypt.Net.BCrypt.HashPassword(request.Password);
            }

            await _userRepository.UpdateUserAsync(user); // ✅ Cập nhật bảng User

            int addressId = 0; // Lưu AddressId để cập nhật bảng Address

            // ✅ 3. Nếu UserType là EMPLOYEE → Cập nhật bảng Employee
            if (user.UserType.ToUpper() == "EMPLOYEE")
            {
                var employee = await _userRepository.GetEmployeeByUserIdAsync(userId);
                if (employee == null)
                    throw new ArgumentException("Employee không tìm thấy.");

                if (!string.IsNullOrWhiteSpace(request.FullName))
                    employee.FullName = request.FullName.Trim();

                // ✅ Lưu AddressId để cập nhật bảng Address
                addressId = employee.AddressId;

                await _userRepository.UpdateEmployeeAsync(employee);
            }

            // ✅ 4. Nếu UserType là AGENCY → Cập nhật bảng AgencyAccount
            else if (user.UserType.ToUpper() == "AGENCY")
            {
                var agency = await _userRepository.GetAgencyAccountByUserIdAsync(userId);
                if (agency == null)
                    throw new ArgumentException("Agency account không tìm thấy.");

                if (!string.IsNullOrWhiteSpace(request.AgencyName))
                    agency.AgencyName = request.AgencyName.Trim();

                // ✅ Lưu AddressId để cập nhật bảng Address
                addressId = agency.AddressId;

                await _userRepository.UpdateAgencyAccountAsync(agency);
            }

            // ✅ 5. Cập nhật bảng Address nếu có AddressId
            if (addressId > 0 && !string.IsNullOrWhiteSpace(request.Street) &&
                !string.IsNullOrWhiteSpace(request.DistrictName) &&
                !string.IsNullOrWhiteSpace(request.ProvinceName) &&
                !string.IsNullOrWhiteSpace(request.WardName))
            {
                var address = await _userRepository.GetAddressByIdAsync(addressId);
                if (address == null)
                    throw new ArgumentException("Không tìm thấy địa chỉ.");

                var (province, district, ward) = await _userRepository.GetLocationIdsAsync(request.ProvinceName, request.DistrictName, request.WardName);

                address.ProvinceId = province.ProvinceId;
                address.DistrictId = district.DistrictId;
                address.WardId = ward.WardId;
                address.Street = request.Street.Trim();

                await _userRepository.UpdateAddressAsync(address);
            }

            return true;
        }

        //ForGotPassword

        public async Task<bool> ForgotPasswordAsync(ForgotPasswordRequest request)
        {
            // ✅ Tìm User theo Email
            var user = await _userRepository.GetUserByEmailAsync(request.Email);
            if (user == null)
            {
                throw new ArgumentException("Không tìm thấy Email.");
            }

            // ✅ Tạo mật khẩu mới ngẫu nhiên
            string newPassword = PasswordHelper.GenerateRandomPassword();
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(newPassword); // Hash trước khi lưu

            // ✅ Cập nhật mật khẩu mới vào database
            user.Password = hashedPassword;
            await _userRepository.UpdateUserAsync(user);

            // ✅ Gửi email chứa mật khẩu mới
            await SendEmailAsync(user.Email, "Password đã thay đổi", $"Password mới của bạn là: {newPassword}");

            return true;
        }

        private async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                var smtpClient = new SmtpClient("smtp.your-email-provider.com") // ✅ Đổi SMTP server phù hợp
                {
                    Port = 587, // Hoặc 465 tùy nhà cung cấp email
                    Credentials = new NetworkCredential("your-email@example.com", "your-email-password"),
                    EnableSsl = true,
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress("your-email@example.com"),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true,
                };

                mailMessage.To.Add(toEmail);
                await smtpClient.SendMailAsync(mailMessage);
            }
            catch (Exception ex)
            {
                throw new Exception("Lỗi không gửi được email: " + ex.Message);
            }
        }


        public async Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordRequest request)
        {
            // ✅ 1. Lấy User từ database
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null)
            {
                throw new ArgumentException("User không tìm thấy.");
            }

            // ✅ 2. Kiểm tra mật khẩu cũ có đúng không (Không hash)
            if (request.OldPassword != user.Password)
            {
                throw new ArgumentException("Mật khẩu cũ không đúng!");
            }

            // ✅ 3. Kiểm tra mật khẩu mới không được trùng với mật khẩu cũ
            if (request.NewPassword == user.Password)
            {
                throw new ArgumentException("Mật khẩu mới không được phép trùng với mật khẩu cũ");
            }

            // ✅ 4. Kiểm tra độ mạnh của mật khẩu mới
            if (!IsValidPassword(request.NewPassword))
            {
                throw new ArgumentException("Mật khẩu mới phải có ít nhất 9 ký tự, chứa ít nhất một chữ cái viết hoa và một ký tự đặc biệt.");
            }

            // ✅ 5. Kiểm tra mật khẩu mới có khớp với xác nhận mật khẩu không
            if (request.NewPassword != request.ConfirmPassword)
            {
                throw new ArgumentException("Mật khẩu mới và xác nhận mật khẩu không khớp.");
            }

            // ✅ 6. Cập nhật mật khẩu vào database (Không hash)
            user.Password = request.NewPassword;

            return await _userRepository.UpdateUserAsync(user);
        }


        // ✅ Hàm kiểm tra độ mạnh của mật khẩu
        private bool IsValidPassword(string password)
        {
            return password.Length >= 9 && // Ít nhất 9 ký tự
                   Regex.IsMatch(password, @"[A-Z]") && // Ít nhất một chữ hoa
                   Regex.IsMatch(password, @"[\W_]"); // Ít nhất một ký tự đặc biệt
        }

        //Edit Role
        public async Task<bool> ChangeEmployeeRoleAsync(Guid userId, int newRoleId)
        {
            // ✅ Kiểm tra roleId hợp lệ
            var validRoles = new List<int> { 3, 4, 5 };
            if (!validRoles.Contains(newRoleId))
                throw new ArgumentException("RoleId phải là 3 (Warehouse), 4 (Sales), or 5 (Accountant)");

            var employee = await _userRepository.GetEmployeeByUserIdAsync(userId);
            if (employee == null)
                throw new ArgumentException("Employee không tìm thấy.");

            var userRole = await _userRepository.GetUserRoleByUserIdAsync(userId);
            if (userRole == null)
                throw new ArgumentException("UserRole không tìm thấy.");

            // ✅ Không được đổi sang cùng role hiện tại
            if (userRole.RoleId == newRoleId)
                throw new ArgumentException("New role giống với current role.");

            // ✅ Đổi Role
            userRole.RoleId = newRoleId;

            // ✅ Cập nhật Department tương ứng
            switch (newRoleId)
            {
                case 3:
                    employee.Department = "WAREHOUSE MANAGER";
                    break;
                case 4:
                    employee.Department = "SALES MANAGER";
                    break;
                case 5:
                    employee.Department = "ACCOUNTANT MANAGER";
                    break;
                case 6:
                    employee.Department = "WAREHOUSE PLANNER";
                    break;
            }

            bool roleUpdated = await _userRepository.UpdateUserRoleAsync(userRole);
            bool empUpdated = await _userRepository.UpdateEmployeeAsync(employee);

            return roleUpdated && empUpdated;
        }

        public async Task<object> LoginAsync(LoginRequest request)
        {
            var user = await _userRepository.GetUserByUsernameAsync(request.userName);
            if (user == null)
            {
                throw new ArgumentException("Tài Khoản của bạn cần phải được kích hoạt!");
            }
            if (user == null || request.Password != user.Password)
            {
                throw new ArgumentException("Tên người dùng hoặc mật khẩu không hợp lệ.");
            }

            if (user.Status == false)
            {
                throw new ArgumentException("Tài khoản của bạn không thể đăng nhập!");
            }


            // Lấy RoleId từ UserRole
            var userRole = await _userRepository.GetUserRoleByUserIdAsync(user.UserId);
            long roleId = userRole?.RoleId ?? 0;
            string roleName = userRole?.Role?.RoleName ?? null;
            string displayName = await _userRepository.GetEmployeeFullNameByUserIdAsync(user.UserId)
                    ?? await _userRepository.GetAgencyNameByUserIdAsync(user.UserId);

            // Tạo JWT Token
            var token = await _jwtService.GenerateJwtTokenAsync(user, roleId);
            return new { roleName, roleId, displayName, token };
        }

        private DateTime GetVnNow()
        {
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
        }

        /*public async Task<object> LoginAsync(LoginRequest request)
        {
            var user = await _userRepository.GetUserByUsernameAsync(request.userName);
            if (user == null || request.Password != user.Password || !user.Status)
                throw new ArgumentException("Tài khoản không hợp lệ");

            var userRole = await _userRepository.GetUserRoleByUserIdAsync(user.UserId);
            long roleId = userRole?.RoleId ?? 0;
            string displayName = await _userRepository.GetEmployeeFullNameByUserIdAsync(user.UserId)
                ?? await _userRepository.GetAgencyNameByUserIdAsync(user.UserId);

            var token = await _jwtService.GenerateJwtTokenAsync(user, roleId);
            var accessTokenExpires = DateTime.UtcNow.AddMinutes(double.Parse(_configuration["Jwt:ExpireMinutes"]));

            // ✅ Gửi cookie HttpOnly
            _httpContextAccessor.HttpContext.Response.Cookies.Append("access_token", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true, // Dùng HTTPS trên môi trường thật
                SameSite = SameSiteMode.Strict,
                Expires = accessTokenExpires
            });

            // ✅ Tạo refresh token
            var refreshToken = Guid.NewGuid().ToString();
            var refreshTokenExpires = GetVnNow().AddDays(15);

            await _refreshTokenRepo.SaveAsync(new RefreshToken
            {
                Token = refreshToken,
                UserId = user.UserId,
                CreatedAt = GetVnNow(),
                ExpiredAt = refreshTokenExpires,
                IsRevoked = false
            });

            _httpContextAccessor.HttpContext.Response.Cookies.Append("refresh_token", refreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Expires = refreshTokenExpires
            });

            return new { roleId, displayName, roleName = userRole?.Role?.RoleName, token };
        }*/


        public async Task<List<RegisterAccountWithContractsDto>> GetRegisterAccount()
        {
            var accounts = await _userRepository.GetRegisterAccount();

            return accounts.Select(a => new RegisterAccountWithContractsDto
            {
                RegisterId = a.RegisterId,
                Username = a.Username,
                Email = a.Email,
                Phone = a.Phone,
                UserType = a.UserType,
                FullName = a.FullName,
                Position = a.Position,
                Department = a.Department,
                AgencyName = a.AgencyName,
                Street = a.Street,
                WardName = a.WardName,
                DistrictName = a.DistrictName,
                ProvinceName = a.ProvinceName,
                IsApproved = a.IsApproved,
                AccountRegisterStatus = a.AccountRegisterStatus,
                Contracts = a.Contracts?.Select(c => new ContractDto
                {
                    ContractId = c.ContractId,
                    FileName = c.FileName,
                    FilePath = c.FilePath,
                    FileType = c.FileType,
                    CreatedAt = c.UploadedAt
                }).ToList() ?? new()
            }).ToList();
        }

        public async Task<long?> GetAgencyIdByUserId(Guid userId)
        {
            return await _userRepository.GetAgencyIdByUserId(userId);
        }

        public async Task<long?> GetEmployeeIdByUserId(Guid userId)
        {
            return await _userRepository.GetEmployeeIdByUserId(userId);
        }

        public async Task<bool> CancelUserAsync(int registerId)
        {
            RegisterAccount registerUser = await _userRepository.GetRegisterAccountByIdAsync(registerId);
            if (registerUser.AccountRegisterStatus == "Approved") return false;

            if (registerUser.AccountRegisterStatus == "Pending")
            {
                registerUser.AccountRegisterStatus = "Canceled";
                await _userRepository.UpdateRegisterAsync(registerUser);
                await _userRepository.SaveAsync();
            }
            return true;
        }

        public async Task<(bool IsSuccess, string Message)> UnActiveUser(Guid userId)
        {
            // Lấy thông tin User từ database
            User user = await _userRepository.GetUserByIdAsync(userId);

            // Kiểm tra nếu user có tồn tại hay không
            if (user == null)
            {
                return (false, "User không tồn tại.");
            }

            // Đảo ngược trạng thái của user
            user.Status = !user.Status;

            // Cập nhật trạng thái mới vào database
            await _userRepository.UpdateUserAsync(user);
            await _userRepository.SaveAsync();

            // Xây dựng message phản hồi dựa trên trạng thái mới
            string message = user.Status
                ? "Tài khoản đã được kích hoạt."
                : "Tài khoản đã bị vô hiệu hóa.";

            // Gửi thông báo real-time SignalR
            await _hub.Clients.User(userId.ToString())
                .SendAsync("UnActive", new
                {
                    title = "Trạng thái tài khoản",
                    payload = userId
                });

            var timeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            var vietnamNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);

            // Lưu vào Notification
            var notification = new Notification
            {
                UserId = userId,
                Title = "Trạng thái tài khoản",
                Message = message,
                Url = $"/agency/profile",
                CreatedAt = vietnamNow
            };

            await _notificationRepository.AddAsync(notification);
            await _notificationRepository.SaveChangesAsync();

            return (true, message);
        }

        public async Task<List<AgencyAccountDto>> GetAgenciesManagedByUserIdAsync(Guid userId)
        {
            var employee = await _userRepository.GetByUserIdAsync(userId);
            if (employee == null)
            {
                throw new Exception("Employee không tìm thấy.");
            }

            var agencies = await _agencyAccountRepository.GetAgenciesManagedByEmployeeIdAsync(employee.EmployeeId);

            var dtoList = agencies.Select(a => new AgencyAccountDto
            {
                AgencyId = a.AgencyId,
                AgencyName = a.AgencyName,
                CreatedAt = a.CreatedAt,
                Email = a.User?.Email,
                Phone = a.User?.Phone,
                Address = $"{a.Address?.Street}, {a.Address?.Ward?.WardName}, {a.Address?.District?.DistrictName}, {a.Address?.Province?.ProvinceName}"
            }).ToList();

            return dtoList;
        }

        public async Task<EmployeeDto> GetSalesManagerByAgencyUserIdAsync(Guid userId)
        {
            // Lấy AgencyAccount dựa trên UserId của đại lý
            var agencyAccount = await _agencyAccountRepository.GetByUserIdAsync(userId);
            if (agencyAccount == null)
                throw new Exception("Không tìm thấy đại lý!");

            // Lấy nhân viên quản lý (ManagedByEmployee)
            var manager = agencyAccount.ManagedByEmployee;
            if (manager == null)
                throw new Exception("Không tìm thấy nhân viên quản lý!");

            // Map sang DTO
            return new EmployeeDto
            {
                UserId = manager.UserId,
                FullName = manager.FullName
            };
        }

    }

}