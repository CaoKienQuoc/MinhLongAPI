using BusinessObject.DTO;
using BusinessObject.Models;
using Microsoft.AspNetCore.Mvc;
using Services.IService;
using Services.Service;

namespace MLHR.Controllers
{
    [Route("api/")]
    [ApiController]
    public class RegisterController : ControllerBase
    {
        private readonly IUserService _userService;

        public RegisterController(IUserService userService)
        {
            _userService = userService;
        }

        
        [HttpGet("auth")]
        public async Task<IActionResult> GetRegisterAccount()
        {
            try
            {
                // Lấy thông tin tài khoản từ service
                var account = await _userService.GetRegisterAccount();

                // Kiểm tra nếu tài khoản không tồn tại
                if (account == null)
                {
                    return NotFound(new { message = "Account does not exist!" });
                }

                return Ok(account);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }


        /// <summary>
        /// Người dùng gửi yêu cầu đăng ký
        /// </summary>
        [HttpPost("auth/register")]
        public async Task<IActionResult> Register([FromForm] RegisterRequest request)
        {
            try
            {
                var registerAccount = await _userService.RegisterUserRequestAsync(request);
                return Ok(new
                {
                    message = "Registration successful, please wait for admin approval!",
                    registerId = registerAccount.RegisterId
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }



        /// <summary>
        /// Admin duyệt tài khoản đăng ký
        /// </summary>
        [HttpPut("auth/register/{id}")]
        public async Task<IActionResult> ApproveUser(int id)
        {
            try
            {
                bool isApproved = await _userService.ApproveUserAsync(id);
                if (!isApproved)
                    return BadRequest(new { message = "Account cannot be approved or account does not exist!" });

                return Ok(new { message = "Account approved successfully!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPut("{registerId}/cancel")]
        public async Task<IActionResult> CancelRequest(int registerId)
        {
            try
            {
                var result = await _userService.CancelUserAsync(registerId);
                if (!result)
                    return BadRequest(new { message = "Failed to cancel RegisterAccount." });

                return Ok(new { message = "RegisterAccount canceled successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /*[HttpPut("{registerId}/UnActive")]
        public async Task<IActionResult> UnActiveRegister(int registerId)
        {
            try
            {
                var result = await _userService.UnActiveRegister(registerId);
                if (!result)
                    return BadRequest(new { message = "Failed to cancel RegisterAccount." });

                return Ok(new { message = "RegisterAccount canceled successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }*/
    }

}
