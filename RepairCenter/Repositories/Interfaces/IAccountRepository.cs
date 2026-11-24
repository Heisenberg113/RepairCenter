using RepairCenter.Entities;
namespace RepairCenter.Repositories.Interfaces;
public interface IAccountRepository
{
    Task<User?> GetByUserNameAsync(string userName);
    Task<User?> GetByIdAsync(int id);
    Task<List<User>> GetAllAsync();
    Task<bool> IdentityExistsAsync(string userName, string email, int? excludeId = null);
    Task<List<Role>> GetRolesAsync(IEnumerable<string> names);
    Task<List<Role>> GetAllRolesAsync();
    Task<bool> HasActiveTechnicianProfileAsync(int userId);
    Task<(User? User, bool LastActiveAdmin)> UpdateRolesSafelyAsync(int userId, IReadOnlyCollection<Role> roles);
    Task<(User? User, bool LastActiveAdmin)> SetActiveSafelyAsync(int userId, bool isActive);
    Task AddAsync(User user, IEnumerable<Role> roles);
    Task SaveAsync();
    Task EnsureBaseRolesAsync();
}
