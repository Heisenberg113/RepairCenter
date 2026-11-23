using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RepairCenter.Entities;

namespace RepairCenter.Data;

public static class AccountBootstrapper
{
    public static async Task InitializeAsync(IServiceProvider services, IConfiguration configuration)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RepairCenterDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();
        foreach (var (name, description) in new[] { ("Admin", "Quản trị hệ thống"), ("Receptionist", "Tiếp nhận và chăm sóc khách hàng"), ("Technician", "Kỹ thuật viên") })
            if (!await db.Roles.AnyAsync(x => x.Name == name)) db.Roles.Add(new Role { Name = name, Description = description });
        await db.SaveChangesAsync();

        var username = configuration["BootstrapAdmin:UserName"];
        var email = configuration["BootstrapAdmin:Email"];
        var password = configuration["BootstrapAdmin:Password"];
        if (string.IsNullOrWhiteSpace(username) && string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(password)) return;
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("BootstrapAdmin requires UserName, Email and Password to all be configured.");
        if (password.Length < 12 || !password.Any(char.IsUpper) || !password.Any(char.IsLower) ||
            !password.Any(char.IsDigit) || !password.Any(ch => !char.IsLetterOrDigit(ch)))
            throw new InvalidOperationException("BootstrapAdmin:Password must be at least 12 characters and include uppercase, lowercase, number and symbol characters.");
        var existing = await db.Users.Include(x => x.UserRoles).ThenInclude(x => x.Role)
            .FirstOrDefaultAsync(x => x.UserName == username);
        if (existing is not null)
        {
            if (existing.IsActive && existing.UserRoles.Any(x => x.Role.Name == "Admin" && x.Role.IsActive)) return;
            throw new InvalidOperationException("The configured BootstrapAdmin user already exists but does not have an active Admin role.");
        }
        if (await db.Users.AnyAsync(x => x.Email == email.Trim().ToLowerInvariant()))
            throw new InvalidOperationException("BootstrapAdmin:Email is already used by another account.");

        var user = new User { UserName = username.Trim(), Email = email.Trim().ToLowerInvariant(), FullName = "System Administrator", PasswordHash = "" };
        user.PasswordHash = hasher.HashPassword(user, password);
        var adminRole = await db.Roles.SingleAsync(x => x.Name == "Admin");
        user.UserRoles.Add(new UserRole { User = user, Role = adminRole });
        db.Users.Add(user);
        await db.SaveChangesAsync();
    }
}
