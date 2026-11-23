using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using RepairCenter.Entities;
using RepairCenter.Repositories.Interfaces;
using RepairCenter.Services.Interfaces;
namespace RepairCenter.Services;
public class AccountService(IAccountRepository repository, IPasswordHasher<User> hasher, IConfiguration configuration) : IAccountService
{


    public async Task<AccountResult<(User User, string Token, DateTime ExpiresAt)>> LoginAsync(string userName, string password)
    {
        var user = await repository.GetByUserNameAsync(userName.Trim());
        if (user is null || hasher.VerifyHashedPassword(user, user.PasswordHash, password) == PasswordVerificationResult.Failed)
            return AccountResult<(User, string, DateTime)>.Failure(AccountError.InvalidCredentials);
        if (!user.IsActive || user.UserRoles.All(x => !x.Role.IsActive))
            return AccountResult<(User, string, DateTime)>.Failure(AccountError.InactiveAccount);
        var hasActiveTechnicianProfile = await repository.HasActiveTechnicianProfileAsync(user.Id);
        var roles = user.UserRoles
            .Where(x => x.Role.IsActive && (x.Role.Name != "Technician" || hasActiveTechnicianProfile))
            .Select(x => x.Role.Name).Distinct().ToArray();
        if (roles.Length == 0)
            return AccountResult<(User, string, DateTime)>.Failure(AccountError.InactiveAccount);
        var expires = DateTime.UtcNow.AddMinutes(configuration.GetValue<int?>("Jwt:AccessTokenMinutes") ?? 60);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName),
            new(ClaimTypes.GivenName, user.FullName),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new("token_version", user.TokenVersion.ToString())
        };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!));
        var token = new JwtSecurityToken(configuration["Jwt:Issuer"], configuration["Jwt:Audience"], claims,
            expires: expires, signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return AccountResult<(User, string, DateTime)>.Success((user, new JwtSecurityTokenHandler().WriteToken(token), expires));
    }

    public async Task<List<User>> GetAllAsync() => await repository.GetAllAsync();
    public async Task<List<string>> GetRolesAsync() => (await repository.GetAllRolesAsync()).Select(x => x.Name).ToList();

    public async Task<AccountResult<User>> CreateAsync(User user, string password, IEnumerable<string> roleNames)
    {
        if (!StrongPassword(password)) return AccountResult<User>.Failure(AccountError.InvalidPassword);
        user.UserName = user.UserName.Trim(); user.Email = user.Email.Trim().ToLowerInvariant();
        user.FullName = user.FullName.Trim(); user.PhoneNumber = Normalize(user.PhoneNumber);
        if (await repository.IdentityExistsAsync(user.UserName, user.Email)) return AccountResult<User>.Failure(AccountError.DuplicateIdentity);
        var roles = await repository.GetRolesAsync(roleNames);
        if (!roleNames.Any() || roles.Count != roleNames.Select(x => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count())
            return AccountResult<User>.Failure(AccountError.InvalidRoles);
        if (roles.Any(x => x.Name == "Technician") && !await repository.HasActiveTechnicianProfileAsync(user.Id))
            return AccountResult<User>.Failure(AccountError.TechnicianProfileRequired);
        user.PasswordHash = hasher.HashPassword(user, password);
        user.MustChangePassword = true;
        await repository.AddAsync(user, roles);
        return AccountResult<User>.Success(user);
    }

    public async Task<AccountResult<User>> SetRolesAsync(int id, IEnumerable<string> roleNames)
    {
        var user = await repository.GetByIdAsync(id);
        if (user is null) return AccountResult<User>.Failure(AccountError.AccountNotFound);
        var names = roleNames.ToArray();
        var roles = await repository.GetRolesAsync(names);
        if (!names.Any() || roles.Count != names.Select(x => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count())
            return AccountResult<User>.Failure(AccountError.InvalidRoles);
        if (roles.Any(x => x.Name == "Technician") && !await repository.HasActiveTechnicianProfileAsync(user.Id))
            return AccountResult<User>.Failure(AccountError.TechnicianProfileRequired);
        if (user.IsActive && user.UserRoles.Any(x => x.Role.IsActive && x.Role.Name == "Admin") &&
            !roles.Any(x => x.Name == "Admin") && await repository.CountActiveAdminsAsync() <= 1)
            return AccountResult<User>.Failure(AccountError.LastActiveAdmin);
        user.UserRoles.Clear();
        foreach (var role in roles) user.UserRoles.Add(new UserRole { UserId = id, RoleId = role.Id, User = user, Role = role });
        user.TokenVersion++;
        await repository.SaveAsync();
        return AccountResult<User>.Success(user);
    }

    public async Task<AccountResult<User>> SetActiveAsync(int id, bool active)
    {
        var user = await repository.GetByIdAsync(id);
        if (user is null) return AccountResult<User>.Failure(AccountError.AccountNotFound);
        if (!active && user.IsActive && user.UserRoles.Any(x => x.Role.IsActive && x.Role.Name == "Admin") &&
            await repository.CountActiveAdminsAsync() <= 1)
            return AccountResult<User>.Failure(AccountError.LastActiveAdmin);
        if (user.IsActive != active)
        {
            user.IsActive = active;
            user.TokenVersion++;
        }
        await repository.SaveAsync();
        return AccountResult<User>.Success(user);
    }

    public async Task<AccountResult<User>> RevokeTokensAsync(int id)
    {
        var user = await repository.GetByIdAsync(id);
        if (user is null) return AccountResult<User>.Failure(AccountError.AccountNotFound);
        user.TokenVersion++;
        await repository.SaveAsync();
        return AccountResult<User>.Success(user);
    }

    public async Task<AccountResult<string>> ResetPasswordAsync(int id)
    {
        var user = await repository.GetByIdAsync(id);
        if (user is null) return AccountResult<string>.Failure(AccountError.AccountNotFound);
        if (!user.IsActive) return AccountResult<string>.Failure(AccountError.InactiveAccount);
        var temporaryPassword = GenerateTemporaryPassword();
        user.PasswordHash = hasher.HashPassword(user, temporaryPassword);
        user.MustChangePassword = true;
        user.TokenVersion++;
        await repository.SaveAsync();
        return AccountResult<string>.Success(temporaryPassword);
    }

    public async Task<AccountResult<User>> ChangeTemporaryPasswordAsync(int id, string currentPassword, string newPassword)
    {
        var user = await repository.GetByIdAsync(id);
        if (user is null) return AccountResult<User>.Failure(AccountError.AccountNotFound);
        if (hasher.VerifyHashedPassword(user, user.PasswordHash, currentPassword) == PasswordVerificationResult.Failed)
            return AccountResult<User>.Failure(AccountError.InvalidCredentials);
        if (!StrongPassword(newPassword)) return AccountResult<User>.Failure(AccountError.InvalidPassword);
        user.PasswordHash = hasher.HashPassword(user, newPassword);
        user.MustChangePassword = false;
        user.TokenVersion++;
        await repository.SaveAsync();
        return AccountResult<User>.Success(user);
    }

    private static string GenerateTemporaryPassword()
    {
        const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string lower = "abcdefghijkmnopqrstuvwxyz";
        const string digits = "23456789";
        const string symbols = "!@#$%*-_+?";
        var groups = new[] { upper, lower, digits, symbols };
        var chars = groups.Select(group => group[RandomNumberGenerator.GetInt32(group.Length)]).ToList();
        const string all = upper + lower + digits + symbols;
        while (chars.Count < 18) chars.Add(all[RandomNumberGenerator.GetInt32(all.Length)]);
        for (var i = chars.Count - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }
        return new string(chars.ToArray());
    }

    private static bool StrongPassword(string value) => value.Length >= 12 && value.Any(char.IsUpper) &&
        value.Any(char.IsLower) && value.Any(char.IsDigit) && value.Any(ch => !char.IsLetterOrDigit(ch));
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
