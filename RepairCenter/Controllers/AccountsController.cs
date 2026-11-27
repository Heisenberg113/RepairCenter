using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RepairCenter.DTOs.Accounts;
using RepairCenter.Entities;
using RepairCenter.Services.Interfaces;

namespace RepairCenter.Controllers;

[ApiController]
[Route("api/accounts")]
[Authorize(Roles = "Admin")]
public class AccountsController(IAccountService accounts) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AccountResponse>>> GetAll() => Ok((await accounts.GetAllAsync()).Select(ToResponse));

    [HttpGet("roles")]
    public async Task<ActionResult<IEnumerable<string>>> GetRoles() => Ok(await accounts.GetRolesAsync());

    [HttpPost]
    public async Task<ActionResult<object>> Create(CreateAccountRequest request)
    {
        var user = new User
        {
            UserName = request.UserName, FullName = request.FullName, Email = request.Email,
            PhoneNumber = request.PhoneNumber, PasswordHash = string.Empty
        };
        var result = await accounts.CreateAsync(user, request.Roles);
        if (result.Error != AccountError.None) return Error(result.Error);
        var created = result.Value;
        return CreatedAtAction(nameof(GetAll), null, new
        {
            account = ToResponse(created.User),
            temporaryPassword = created.TemporaryPassword
        });
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<AccountResponse>> Update(int id, UpdateAccountRequest request)
    {
        var result = await accounts.UpdateAsync(
            id, request.UserName, request.FullName, request.Email, request.PhoneNumber);
        return result.Error == AccountError.None ? Ok(ToResponse(result.Value!)) : Error(result.Error);
    }

    [HttpPut("{id:int}/roles")]
    public async Task<ActionResult<AccountResponse>> SetRoles(int id, UpdateAccountRolesRequest request)
    {
        var result = await accounts.SetRolesAsync(id, request.Roles);
        return result.Error == AccountError.None ? Ok(ToResponse(result.Value!)) : Error(result.Error);
    }

    [HttpPatch("{id:int}/active")]
    public async Task<ActionResult<AccountResponse>> SetActive(int id, SetAccountActiveRequest request)
    {
        var result = await accounts.SetActiveAsync(id, request.IsActive);
        return result.Error == AccountError.None ? Ok(ToResponse(result.Value!)) : Error(result.Error);
    }

    [HttpPost("{id:int}/revoke-tokens")]
    public async Task<IActionResult> RevokeTokens(int id)
    {
        var result = await accounts.RevokeTokensAsync(id);
        return result.Error == AccountError.None ? NoContent() : Error(result.Error);
    }

    [HttpPost("{id:int}/reset-password")]
    public async Task<ActionResult<ResetPasswordResponse>> ResetPassword(int id)
    {
        var result = await accounts.ResetPasswordAsync(id);
        return result.Error == AccountError.None
            ? Ok(new ResetPasswordResponse { TemporaryPassword = result.Value! })
            : Error(result.Error);
    }

    private ActionResult Error(AccountError error) => error switch
    {
        AccountError.AccountNotFound => NotFound(),
        AccountError.InactiveAccount => Conflict(new ProblemDetails { Title = "Không thể đặt lại mật khẩu cho tài khoản đã khóa." }),
        AccountError.DuplicateIdentity => Conflict(new ProblemDetails { Title = "Tên đăng nhập hoặc email đã được sử dụng." }),
        AccountError.InvalidRoles => BadRequest(new ProblemDetails { Title = "Danh sách role không hợp lệ hoặc không hoạt động." }),
        AccountError.InvalidPassword => BadRequest(new ProblemDetails { Title = "Mật khẩu cần ít nhất 12 ký tự, gồm chữ hoa, chữ thường, số và ký tự đặc biệt." }),
        AccountError.LastActiveAdmin => Conflict(new ProblemDetails { Title = "Không thể gỡ quyền hoặc khóa Admin cuối cùng đang hoạt động." }),
        AccountError.TechnicianProfileRequired => Conflict(new ProblemDetails { Title = "Tài khoản cần được liên kết với hồ sơ Technician trước khi gán role Technician." }),
        _ => Problem(statusCode: StatusCodes.Status500InternalServerError)
    };

    private static AccountResponse ToResponse(User user) => new()
    {
        Id = user.Id, UserName = user.UserName, FullName = user.FullName, Email = user.Email,
        PhoneNumber = user.PhoneNumber, IsActive = user.IsActive, CreatedAt = user.CreatedAt,
        Roles = user.UserRoles.Where(x => x.Role.IsActive).Select(x => x.Role.Name).Distinct().ToArray()
    };
}
