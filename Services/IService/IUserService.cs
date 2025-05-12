using BusinessObject.DTO;
using BusinessObject.DTO.Warehouse;
using BusinessObject.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.IService
{
    public interface IUserService
    {
        Task<RegisterAccount> RegisterUserRequestAsync(RegisterRequest request);
        Task<bool> ApproveUserAsync(int registerId);
        //Login
        //Task<User> LoginAsync(string email, string password);
        //Logout
        Task<bool> LogoutAsync(string email);
        Task<bool> UpdateUserAccountAsync(Guid userId, UpdateUserRequest request);

        Task<bool> ForgotPasswordAsync(ForgotPasswordRequest request);

        Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordRequest request);
        Task<bool> ChangeEmployeeRoleAsync(Guid userId, int newRoleId);
        Task<object> LoginAsync(LoginRequest request);
        Task<PagedResult<UserDto>> GetUsersAsync();
        Task<List<RegisterAccountWithContractsDto>> GetRegisterAccount();
        Task<long?> GetAgencyIdByUserId(Guid userId);
        Task<long?> GetEmployeeIdByUserId(Guid userId);
        Task<bool> CancelUserAsync(int registerId);
        Task<(bool IsSuccess, string Message)> UnActiveUser(Guid userId);
        Task<User> GetUserByIdAsync(Guid userId);

        Task<UserDetailDto> GetAgencyUserByIdAsync(Guid userId);
        Task<List<AgencyAccountDto>> GetAgenciesManagedByUserIdAsync(Guid userId);
        Task<EmployeeDto> GetSalesManagerByAgencyUserIdAsync(Guid userId);
    }


}
