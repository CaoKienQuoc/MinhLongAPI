using BusinessObject.DTO;
using BusinessObject.Models;
using DataAccessLayer;
using Microsoft.EntityFrameworkCore;
using Repo.IRepository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repo.Repository
{
    public class UserRepository : IUserRepository
    {
        private readonly MinhLongDbContext _context;

        public UserRepository(MinhLongDbContext context)
        {
            _context = context;
        }


        public async Task<int> GetTotalUsersAsync() // ✅ Triển khai phương thức này
        {
            return await _context.Users.CountAsync();
        }

        public async Task<List<User>> GetUsersAsync()
        {
            return await _context.Users
                    .Include(u => u.Employee) // Lấy thông tin Employee của User

                    .ToListAsync();
        }

        public List<UserRole> GetUserRoles(Guid userId)
        {
            return _context.UserRoles.Where(ur => ur.UserId == userId).ToList();
        }

        // ✅ Lưu yêu cầu đăng ký vào RegisterAccount
        public async Task<RegisterAccount> RegisterUserRequestAsync(RegisterAccount registerAccount)
        {
            try
            {
                // ✅ Tạo mật khẩu ngẫu nhiên 9 ký tự
                string rawPassword = registerAccount.Password;

                //registerAccount.Password = BCrypt.Net.BCrypt.HashPassword(rawPassword);
                // ✅ Kiểm tra nếu Username, Email hoặc Phone đã tồn tại
                var existingAccount = await _context.RegisterAccounts
                    .FirstOrDefaultAsync(u => u.Username == registerAccount.Username ||
                                              u.Email == registerAccount.Email ||
                                              u.Phone == registerAccount.Phone);
                if (existingAccount != null)
                {
                    throw new ArgumentException("Username, Email, or Phone already exists!");
                }


                // ✅ Nếu Password rỗng hoặc null, đặt giá trị mặc định là "1"
                if (string.IsNullOrWhiteSpace(registerAccount.Password))
                {
                    registerAccount.Password = rawPassword;
                }
                // ✅ Lưu tài khoản đăng ký vào RegisterAccount
                _context.RegisterAccounts.Add(registerAccount);
                await _context.SaveChangesAsync();

                return registerAccount;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                throw new Exception("Error saving registration account: " + ex.Message);
            }
        }

        // ✅ Admin duyệt tài khoản và lưu vào bảng User + Employee hoặc Agency
        public async Task<bool> ApproveUserAsync(int registerId)
        {

            try
            {
                var registerAccount = await _context.RegisterAccounts.FindAsync(registerId);
                if (registerAccount.IsApproved == true)
                {
                    throw new Exception("Account was previously activated!");
                }
                else
                {

                    if (registerAccount == null || registerAccount.IsApproved)
                        return false; // Tài khoản không tồn tại hoặc đã được duyệt trước đó

                    // ✅ Tạo Address dựa trên thông tin từ RegisterAccount
                    (Province province, District district, Ward ward) = await GetLocationIdsAsync(
                        registerAccount.ProvinceName, registerAccount.DistrictName, registerAccount.WardName
                    );

                    var newAddress = new Address
                    {
                        Street = registerAccount.Street,
                        WardId = ward.WardId,
                        DistrictId = district.DistrictId,
                        ProvinceId = province.ProvinceId
                    };

                    _context.Addresses.Add(newAddress);
                    await _context.SaveChangesAsync(); // Lưu để lấy AddressId

                    // ✅ Tạo User
                    var user = new User
                    {
                        Username = registerAccount.Username,
                        Email = registerAccount.Email,
                        Phone = registerAccount.Phone,
                        UserType = registerAccount.UserType,
                        Password = registerAccount.Password, // Có thể hash trước khi lưu
                        Status = true
                    };

                    _context.Users.Add(user);
                    await _context.SaveChangesAsync(); // Lưu trước để có UserId

                    int roleId = 0;

                    // ✅ Nếu UserType là EMPLOYEE, lưu vào bảng Employee
                    if (registerAccount.UserType.Equals("EMPLOYEE", StringComparison.OrdinalIgnoreCase))
                    {
                        var employee = new Employee
                        {
                            UserId = user.UserId,
                            FullName = registerAccount.FullName,
                            Position = registerAccount.Position,
                            Department = registerAccount.Department,
                            AddressId = newAddress.AddressId // Sử dụng AddressId thay vì LocationId
                        };

                        _context.Employees.Add(employee);
                        await _context.SaveChangesAsync();

                        var registerContracts = await _context.RegisterAccountContract
                           .Where(rc => rc.RegisterId == registerAccount.RegisterId)
                           .ToListAsync();

                        foreach (var rc in registerContracts)
                        {
                            var contract = new Contract
                            {
                                EmployeeId = employee.EmployeeId,
                                FileName = rc.FileName,
                                FilePath = rc.FilePath,
                                FileType = rc.FileType,
                                CreatedAt = rc.UploadedAt
                            };
                            _context.Contract.Add(contract);
                        }

                        if (registerAccount.Department.Equals("WAREHOUSE MANAGER", StringComparison.OrdinalIgnoreCase))
                        {
                            roleId = 3;
                        }
                        if (registerAccount.Department.Equals("SALES MANAGER", StringComparison.OrdinalIgnoreCase))
                        {
                            roleId = 4;
                        }
                        if (registerAccount.Department.Equals("ACCOUNTANT MANAGER", StringComparison.OrdinalIgnoreCase))
                        {
                            roleId = 5;
                        }
                        if (registerAccount.Department.Equals("WAREHOUSE PLANNER", StringComparison.OrdinalIgnoreCase))
                        {
                            roleId = 6;
                        }
                    }
                    // ✅ Nếu UserType là AGENCY, lưu vào bảng AgencyAccount
                    else if (registerAccount.UserType.Equals("AGENCY", StringComparison.OrdinalIgnoreCase))
                    {
                        var agency = new AgencyAccount
                        {
                            UserId = user.UserId,
                            AgencyName = registerAccount.AgencyName,
                            AddressId = newAddress.AddressId
                        };

                        _context.AgencyAccounts.Add(agency);
                        await _context.SaveChangesAsync(); // ✅ Lưu để có AgencyId

                        // 💡 Lấy danh sách hợp đồng từ RegisterAccountContract
                        var registerContracts = await _context.RegisterAccountContract
                            .Where(rc => rc.RegisterId == registerAccount.RegisterId)
                            .ToListAsync();

                        foreach (var rc in registerContracts)
                        {
                            var contract = new Contract
                            {
                                AgencyId = agency.AgencyId,
                                FileName = rc.FileName,
                                FilePath = rc.FilePath,
                                FileType = rc.FileType,
                                CreatedAt = rc.UploadedAt
                            };
                            _context.Contract.Add(contract);
                        }

                        roleId = 2;
                    }

                    if (roleId > 0)
                    {
                        var userRole = new UserRole
                        {
                            UserId = user.UserId,
                            RoleId = roleId

                        };
                        _context.UserRoles.Add(userRole);
                    }

                    // ✅ Đánh dấu tài khoản đã được duyệt
                    registerAccount.IsApproved = true;
                    registerAccount.AccountRegisterStatus = "Approved";
                    /*// ✅ Xóa RegisterAccount sau khi duyệt
                    _context.RegisterAccounts.Remove(registerAccount);*/
                    await _context.SaveChangesAsync();

                    return true;
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                throw new Exception("Error while approving user: " + ex.Message);
            }

        }



        // ✅ Phương thức lấy Province, District, Ward dựa trên tên
        public async Task<(Province, District, Ward)> GetLocationIdsAsync(string provinceName, string districtName, string wardName)
        {
            var province = await _context.Provinces.FirstOrDefaultAsync(p => p.ProvinceName == provinceName);
            if (province == null) throw new Exception($"Province '{provinceName}' not found.");

            var district = await _context.Districts.FirstOrDefaultAsync(d => d.DistrictName == districtName && d.ProvinceId == province.ProvinceId);
            if (district == null) throw new Exception($"District '{districtName}' not found in Province '{provinceName}'.");

            var ward = await _context.Wards.FirstOrDefaultAsync(w => w.WardName == wardName && w.DistrictId == district.DistrictId);
            if (ward == null) throw new Exception($"Ward '{wardName}' not found in District '{districtName}'.");

            return (province, district, ward);
        }


        // ✅ Cập nhật User (bao gồm mật khẩu)
        /*public async Task<bool> UpdateUserAsync(User user)
        {
            if (!string.IsNullOrWhiteSpace(user.Password))
            {
                user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);
            }
            _context.Users.Update(user);
            return await _context.SaveChangesAsync() > 0;
        }*/


        public async Task<List<User>> GetUsersWithAgencyDetailsAsync()
        {
            return await _context.Users
                .Include(u => u.AgencyAccount)
                    .ThenInclude(a => a.Address)
                    .ThenInclude(a => a.Ward)
                    .ThenInclude(a => a.District)
                    .ThenInclude(a => a.Province)
                .Include(u => u.AgencyAccount)
                    .ThenInclude(a => a.Contracts)
                    .Include(u => u.Employee)
                    .ThenInclude(a => a.Contracts)
                .Include(u => u.Employee)
                    .ThenInclude(a => a.Address)
                    .ThenInclude(a => a.Ward)
                    .ThenInclude(a => a.District)
                    .ThenInclude(a => a.Province)
                .ToListAsync();
        }

        public async Task<Employee> GetEmployeeByUserIdAsync(Guid userId)
        {
            return await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId);
        }

        public async Task<AgencyAccount> GetAgencyAccountByUserIdAsync(Guid? userId)
        {
            return await _context.AgencyAccounts
                .Include(a => a.Contracts) // ✅ thêm Include
        .FirstOrDefaultAsync(a => a.UserId == userId);
        }

        public async Task<Guid?> GetUserIdByAgencyIdAsync(long agencyId)
        {
            var agencyAccount = await _context.AgencyAccounts
                .Where(a => a.AgencyId == agencyId)
                .Select(a => a.UserId)
                .FirstOrDefaultAsync();

            return agencyAccount;
        }



        public async Task<Address> GetAddressByIdAsync(int addressId)
        {
            return await _context.Addresses.FirstOrDefaultAsync(a => a.AddressId == addressId);
        }

        public async Task<bool> UpdateAddressAsync(Address address)
        {
            _context.Addresses.Update(address);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> UpdateUserAsync(User user)
        {
            _context.Users.Update(user);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> UpdateEmployeeAsync(Employee employee)
        {
            _context.Employees.Update(employee);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> UpdateAgencyAccountAsync(AgencyAccount agencyAccount)
        {
            _context.AgencyAccounts.Update(agencyAccount);
            return await _context.SaveChangesAsync() > 0;
        }


        // ✅ Tìm User theo UserId
        public async Task<User> GetUserByIdAsync(Guid userId)
        {
            // Lấy thông tin người dùng từ bảng User, và include dữ liệu từ bảng Employee hoặc Agency
            var user = await _context.Users
                .Include(u => u.Employee) // Bao gồm thông tin Employee (nếu có)
                .Include(u => u.AgencyAccount) // Bao gồm thông tin Agency (nếu có)
                .FirstOrDefaultAsync(u => u.UserId == userId);
            return user;
        }


        public async Task<Employee> GetByEmployeeUserIdAsync(Guid userId)
        {
            return await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId);
        }

        public async Task<User> GetEmployeeByIdAsync(Guid userId)
        {
            return await _context.Users
                .Where(u => u.UserId == userId)
                .Include(u => u.Employee)
                    .ThenInclude(e => e.Address)
                        .ThenInclude(a => a.Ward)
                            .ThenInclude(w => w.District)
                                .ThenInclude(d => d.Province)
                .FirstOrDefaultAsync();
        }


        // Lấy thông tin RegisterAccount theo ID
        public async Task<RegisterAccount> GetRegisterAccountByIdAsync(int registerId)
        {
            return await _context.RegisterAccounts.FindAsync(registerId);
        }
        //Login
        public async Task<User> LoginAsync(string userName, string password)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == userName);

            if (user == null)
            {
                throw new ArgumentException("Invalid userName or password.");
            }

            // ✅ Nếu mật khẩu đã hash bằng BCrypt, kiểm tra bằng BCrypt
            if (!BCrypt.Net.BCrypt.Verify(password, user.Password))
            {
                throw new ArgumentException("Invalid userName or password.");
            }

            return user;
        }

        //Logout
        public async Task<User> GetUserByEmailAsync(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        }

        //Edit Role
        public async Task<UserRole> GetUserRoleByUserIdAsync(Guid userId)
        {
            return await _context.UserRoles
                .Include(ur => ur.Role) // Load thông tin Role
                .FirstOrDefaultAsync(ur => ur.UserId == userId);
        }

        public async Task<bool> UpdateUserRoleAsync(UserRole userRole)
        {
            _context.UserRoles.Update(userRole);
            return await _context.SaveChangesAsync() > 0;
        }
        public async Task<User> GetUserByUsernameAsync(string username)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        }

        public async Task<List<RegisterAccount>> GetRegisterAccount()
        {
            return await _context.RegisterAccounts
                        .Include(r => r.Contracts)
                        .ToListAsync();
        }

        public async Task<long?> GetAgencyIdByUserId(Guid userId)
        {
            return await _context.AgencyAccounts
                .Where(a => a.UserId == userId)
                .Select(a => (long?)a.AgencyId) // Chỉ lấy AgencyId thay vì toàn bộ object
                .FirstOrDefaultAsync();
        }

        public async Task<long?> GetEmployeeIdByUserId(Guid userId)
        {
            var employeeAccount = await _context.Employees
                .FirstOrDefaultAsync(a => a.UserId == userId); // Truy vấn bằng GUID

            return employeeAccount?.EmployeeId; // Trả về AgencyId (int)
        }

        public async Task UpdateRegisterAsync(RegisterAccount registerAccount)
        {
            _context.RegisterAccounts.Update(registerAccount);
        }

        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task<OTPEmail> AddOTPEmail(OTPEmail otpemail)
        {
            _context.OTPEmails.Add(otpemail);
            await _context.SaveChangesAsync();
            return otpemail;
        }
        public async Task<OTPEmail> GetOTPEmailByEmail(string email)
        {
            var otpEmail = await _context.OTPEmails.FirstOrDefaultAsync(a => a.Email == email);
            return otpEmail;
        }

        public async Task<User> GetUserByUserName(string userName)
        {
            var user = await _context.Users.FirstOrDefaultAsync(a => a.Username == userName);
            return user;
        }

        public async Task<string> GetAgencyNameByUserIdAsync(Guid userId)
        {
            var agencyName = await _context.Users
                .Where(u => u.UserId == userId)
                .Include(u => u.AgencyAccount)
                .Select(u => u.AgencyAccount.AgencyName)
                .FirstOrDefaultAsync();

            return agencyName ?? "Không xác định";
        }


        public async Task<AgencyAccountLevel?> GetLatestLevelByAgencyIdAsync(long agencyId)
        {
            return await _context.AgencyAccountLevels
                .Include(a => a.Level)
                .Where(a => a.AgencyId == agencyId)
                .OrderByDescending(a => a.ChangeDate)
                .FirstOrDefaultAsync();
        }

        public async Task<List<Employee>> GetEmployeesByRoleAsync(string roleName)
        {
            return await _context.Employees
                .Where(e => e.User.UserRoles.Any(ur => ur.Role.RoleName.ToUpper() == roleName.ToUpper()))
                .ToListAsync();
        }

        public async Task<Employee> GetByUserIdAsync(Guid userId)
        {
            return await _context.Employees
                .FirstOrDefaultAsync(e => e.UserId == userId);
        }

        public Task<bool> ExistsAsync(Guid employeeId)
        => _context.Employees.AnyAsync(e => e.UserId == employeeId);

        public async Task<User> GetByIdAsync(Guid userId)
        {
            return await _context.Users
        .Include(u => u.AgencyAccount)
        .FirstOrDefaultAsync(u => u.UserId == userId);
        }


        public async Task<Guid?> GetUserIdByWarehouseIdAsync(long warehouseId)
        {
            return await _context.Warehouses
         .Where(w => w.WarehouseId == warehouseId)
         .Select(w => (Guid?)w.UserId)
         .FirstOrDefaultAsync();
        }

        public async Task<string?> GetEmployeeFullNameByUserIdAsync(Guid userId)
        {
            return await _context.Employees
                .Where(e => e.UserId == userId)
                .Select(e => e.FullName)
                .FirstOrDefaultAsync();
        }
        public async Task<AgencyAccount> GetAgencyAccountByIdAsync(long agencyId)
        {
            return await _context.AgencyAccounts
                .FirstOrDefaultAsync(x => x.AgencyId == agencyId);
        }

        public async Task<List<User>> GetUsersWithEmployeeAsync()
        {
            return await _context.Users
                .Include(u => u.Employee)
                .ToListAsync();
        }

        public async Task<int> CountRegisterAccountsByStatusAsync(string status)
        {
            return await _context.RegisterAccounts
                .Where(r => r.AccountRegisterStatus == status)
                .CountAsync();
        }

        public async Task<int> CountTotalRegisterAccountsAsync()
        {
            return await _context.RegisterAccounts.CountAsync();
        }

        public async Task<List<Warehouse>> GetWarehousesByUserIdAsync(Guid userId)
        {
            return await _context.Warehouses
                .Where(w => w.UserId == userId)
                .ToListAsync();
        }



    }




    public static class PasswordHelper
    {
        public static string GenerateRandomPassword()
        {
            const int length = 9; // ✅ Cố định độ dài là 9 ký tự
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
            Random random = new Random();

            return new string(Enumerable.Repeat(chars, length)
                                        .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}