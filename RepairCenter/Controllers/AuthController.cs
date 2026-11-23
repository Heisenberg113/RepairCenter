using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using RepairCenter.DTOs.Auth;
using RepairCenter.Services.Interfaces;
using System.Security.Claims;

namespace RepairCenter.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IAccountService accounts) : ControllerBase
{
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        var result = await accounts.LoginAsync(request.UserName, request.Password);
        if (result.Error != AccountError.None)
            return Unauthorized(new ProblemDetails { Title = "Thông tin đăng nhập không hợp lệ hoặc tài khoản đã bị khóa." });
        var (user, token, expires) = result.Value;
        return Ok(new LoginResponse
        {
            AccessToken = token, ExpiresAtUtc = expires, UserId = user.Id,
            UserName = user.UserName, FullName = user.FullName,
            Roles = user.UserRoles.Where(x => x.Role.IsActive).Select(x => x.Role.Name).Distinct().ToArray(),
            MustChangePassword = user.MustChangePassword
        });
    }

    [Authorize]
    [HttpPost("change-temporary-password")]
    public async Task<IActionResult> ChangeTemporaryPassword(ChangeTemporaryPasswordRequest request)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Unauthorized();
        var result = await accounts.ChangeTemporaryPasswordAsync(userId, request.CurrentPassword, request.NewPassword);
        return result.Error switch
        {
            AccountError.None => NoContent(),
            AccountError.InvalidCredentials => BadRequest(new ProblemDetails { Title = "Mật khẩu tạm không chính xác." }),
            AccountError.InvalidPassword => BadRequest(new ProblemDetails { Title = "Mật khẩu mới cần ít nhất 12 ký tự, gồm chữ hoa, chữ thường, số và ký tự đặc biệt." }),
            _ => NotFound()
        };
    }

    [Authorize]
    [HttpPost("revoke")]
    public async Task<IActionResult> RevokeCurrentTokens()
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized();
        var result = await accounts.RevokeTokensAsync(userId);
        return result.Error == AccountError.None ? NoContent() : Unauthorized();
    }
}
