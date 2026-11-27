using RepairCenter.Entities;
namespace RepairCenter.Services.Interfaces;
public interface IAccountService
{
    Task<AccountResult<(User User, string Token, DateTime ExpiresAt)>> LoginAsync(string userName, string password);
    Task<List<User>> GetAllAsync();
    Task<User?> GetByIdAsync(int id);
    Task<List<string>> GetRolesAsync();
    Task<AccountResult<(User User, string TemporaryPassword)>> CreateAsync(User user, IEnumerable<string> roles);
    Task<AccountResult<User>> UpdateAsync(int id, string userName, string fullName, string email, string? phoneNumber);
    Task<AccountResult<User>> SetRolesAsync(int id, IEnumerable<string> roles);
    Task<AccountResult<User>> SetActiveAsync(int id, bool active);
    Task<AccountResult<User>> RevokeTokensAsync(int id);
    Task<AccountResult<string>> ResetPasswordAsync(int id);
    Task<AccountResult<User>> ChangeTemporaryPasswordAsync(int id, string currentPassword, string newPassword);
}
