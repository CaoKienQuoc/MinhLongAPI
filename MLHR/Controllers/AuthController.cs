using System.Security.Claims;
using BusinessObject.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.IService;
using Services.Service;
using System.Security.Claims;
using BusinessObject.DTO.Email;

namespace MLHR.Controllers
{
    [Route("api/")]
    [ApiController]
    public class AuthController : Controller
    {
        private readonly IUserService _userService;
        private readonly IEmailService _emailService;

        public AuthController(IUserService userService, IEmailService emailService)
        {
            _userService = userService;
            _emailService = emailService;
        }

        /// <summary>
        /// Đăng nhập bằng Username & Password
        /// </summary>
        [HttpPost("auth/login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                var token = await _userService.LoginAsync(request);
                return Ok(new { token });
            }
            catch (ArgumentException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }


        //logout 
        [HttpPost("auth/logout")]
        public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
        {
            try
            {
                bool isLoggedOut = await _userService.LogoutAsync(request.Email);
                if (!isLoggedOut)
                {
                    return BadRequest(new { message = "Logout failed!" });
                }

                return Ok(new { message = "Logout successful!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [Authorize]
        [HttpPut("user")]
        public async Task<IActionResult> UpdateUser([FromBody] UpdateUserRequest request)
        {
            try
            {
                // ✅ Lấy UserId từ Token
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim))
                {
                    return Unauthorized(new { message = "User is not authenticated." });
                }

                Guid userId = Guid.Parse(userIdClaim); // Chuyển đổi thành GUID

                bool isUpdated = await _userService.UpdateUserAccountAsync(userId, request);
                if (!isUpdated)
                    return BadRequest(new { message = "Update failed!" });

                return Ok(new { message = "User account updated successfully!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }



        /*[HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            try
            {
                bool isSuccessful = await _userService.ForgotPasswordAsync(request);
                if (!isSuccessful)
                    return BadRequest(new { message = "Failed to reset password!" });

                return Ok(new { message = "A new password has been sent to your email!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }*/


        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            try
            {
                // ✅ Lấy UserId từ Token
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim))
                {
                    return Unauthorized(new { message = "User is not authenticated." });
                }

                Guid userId = Guid.Parse(userIdClaim); // Chuyển đổi thành GUID

                bool isSuccessful = await _userService.ChangePasswordAsync(userId, request);
                if (!isSuccessful)
                    return BadRequest(new { message = "Failed to change password!" });

                return Ok(new { message = "Password changed successfully!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [Authorize(Roles = "1")] // Chỉ Admin
        [HttpPut("change-employee-role/{userId}/{newRoleId}")]
        public async Task<IActionResult> ChangeEmployeeRole(Guid userId, int newRoleId)
        {
            try
            {
                bool isSuccessful = await _userService.ChangeEmployeeRoleAsync(userId, newRoleId);
                if (!isSuccessful)
                    return BadRequest(new { message = "Failed to change role!" });

                return Ok(new { message = "Employee role changed successfully!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("user")]
        public async Task<IActionResult> GetUsers()
        {
            var pagedUsers = await _userService.GetUsersAsync();

            var result = new
            {
                Users = pagedUsers.Items.Select(u => new
                {
                    UserId = u.UserId,
                    Username = u.Username,
                    Password = u.Password,
                    UserType = u.UserType,
                    Phone = u.Phone,
                    Email = u.Email,
                    Status = u.Status,
                    Employee = u.Employee != null ? new
                    {
                        Department = u.Employee.Department
                    } : null
                })
            };

            return Ok(result);
        }


        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetById(Guid userId)
        {
            var userDto = await _userService.GetAgencyUserByIdAsync(userId);
            return Ok(userDto);
        }



        /*   [HttpGet("user/{userId}")]
           public async Task<ActionResult<UserDto>> GetProduct(Guid userId)
           {
               var user = await _userService.GetUserByIdAsync(userId);
               if (user == null)
               {
                   return NotFound();
               }
               return Ok(user);
           }*/

        [HttpPut("{userId}/UnActive")]
        public async Task<IActionResult> UnActiveUser(Guid userId)
        {
            var result = await _userService.UnActiveUser(userId);

            if (!result.IsSuccess)
            {
                return NotFound(new { success = false, message = result.Message });
            }

            return Ok(new { message = result.Message });
        }

        [HttpPost("send-otp-email")]
        public async Task<IActionResult> SendOTPToEmail([FromBody] SendOtpEmailRequest sendOtpEmailRequest)
        {
            try
            {
                bool isSendOtp = await _emailService.SendEmailAsync(sendOtpEmailRequest);
                if (!isSendOtp)
                {
                    return BadRequest("Cannot send mail!");
                }
                return Ok("Send Otp successfully!");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("check-otp-email")]
        public async Task<IActionResult> checkOtp([FromBody] CheckOtpRequest CheckOtpRequest)
        {
            try
            {
                bool isValidOtp = await _emailService.CheckOtpEmail(CheckOtpRequest);
                if (!isValidOtp)
                {
                    return BadRequest(new { message = "Otp không đúng. Vui lòng nhập lại" });
                }
                return Ok("Otp is valid!");
            }
            catch (Exception ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [Authorize(Roles = "4")]
        [HttpGet("manage-agency")]
        public async Task<IActionResult> GetManagedAgencies()
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());

            try
            {
                var agencyDtos = await _userService.GetAgenciesManagedByUserIdAsync(userId);
                return Ok(agencyDtos);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}