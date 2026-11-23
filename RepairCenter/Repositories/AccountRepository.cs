using Microsoft.EntityFrameworkCore;
using RepairCenter.Data;
using RepairCenter.Entities;
using RepairCenter.Repositories.Interfaces;
namespace RepairCenter.Repositories;
public class AccountRepository(RepairCenterDbContext db) : IAccountRepository
{
    public Task<User?> GetByUserNameAsync(string name) => db.Users.Include(x => x.UserRoles).ThenInclude(x => x.Role).FirstOrDefaultAsync(x => x.UserName == name);
    public Task<User?> GetByIdAsync(int id) => db.Users.Include(x => x.UserRoles).ThenInclude(x => x.Role).FirstOrDefaultAsync(x => x.Id == id);
    public Task<List<User>> GetAllAsync() => db.Users.AsNoTracking().Include(x => x.UserRoles).ThenInclude(x => x.Role).OrderBy(x => x.Id).ToListAsync();
    public Task<bool> IdentityExistsAsync(string userName, string email, int? excludeId = null)
    {
        var query = db.Users.AsQueryable();
        if (excludeId.HasValue) query = query.Where(x => x.Id != excludeId.Value);
        return query.AnyAsync(x => x.UserName == userName || x.Email == email);
    }
    public Task<List<Role>> GetRolesAsync(IEnumerable<string> names)
    {
        var wanted = names.Select(x => x.Trim().ToUpper()).Distinct().ToArray();
        return db.Roles.Where(x => wanted.Contains(x.Name.ToUpper()) && x.IsActive).ToListAsync();
    }
    public Task<List<Role>> GetAllRolesAsync() => db.Roles.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name).ToListAsync();
    public Task<int> CountActiveAdminsAsync() => db.Users.CountAsync(user => user.IsActive &&
        user.UserRoles.Any(link => link.Role.IsActive && link.Role.Name == "Admin"));
    public Task<bool> HasActiveTechnicianProfileAsync(int userId) =>
        db.Technicians.AnyAsync(x => x.UserId == userId && x.IsActive);
    public async Task AddAsync(User user, IEnumerable<Role> roles)
    {   
        foreach (var role in roles) user.UserRoles.Add(new UserRole { User = user, RoleId = role.Id, Role = role });
        await db.Users.AddAsync(user);
        await db.SaveChangesAsync();
    }
    public Task SaveAsync() => db.SaveChangesAsync();
    public async Task EnsureBaseRolesAsync()
    {
        var names = new[] { ("Admin", "Quản trị hệ thống"), ("Receptionist", "Tiếp nhận và chăm sóc khách hàng"), ("Technician", "Kỹ thuật viên") };
        foreach (var (name, description) in names)
            if (!await db.Roles.AnyAsync(x => x.Name == name))
                db.Roles.Add(new Role { Name = name, Description = description });
        await db.SaveChangesAsync();
    }
}
