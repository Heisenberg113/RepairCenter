using Microsoft.EntityFrameworkCore;
using RepairCenter.Data;
using RepairCenter.Entities;
using RepairCenter.Repositories.Interfaces;

namespace RepairCenter.Repositories;

public class TechnicianRepository(RepairCenterDbContext context) : ITechnicianRepository
{
    public Task<List<Technician>> GetAllAsync() => context.Technicians
        .AsNoTracking()
        .Include(technician => technician.DeviceTypes)
            .ThenInclude(link => link.DeviceType)
        .OrderBy(technician => technician.EmployeeCode)
        .ToListAsync();

    public Task<Technician?> GetByIdAsync(int id) => context.Technicians
        .Include(technician => technician.DeviceTypes)
            .ThenInclude(link => link.DeviceType)
        .FirstOrDefaultAsync(technician => technician.Id == id);

    public Task<List<DeviceType>> GetAllDeviceTypesAsync() => context.DeviceTypes
        .AsNoTracking()
        .OrderBy(type => type.Name)
        .ToListAsync();

    public Task<List<DeviceType>> GetDeviceTypesByIdsAsync(IReadOnlyCollection<int> ids) => context.DeviceTypes
        .Where(type => ids.Contains(type.Id))
        .ToListAsync();

    public async Task<bool> UserAvailableAsync(int? userId, int? exceptTechnicianId = null)
    {
        if (!userId.HasValue)
            return true;

        if (!await context.Users.AnyAsync(user => user.Id == userId.Value))
            return false;

        var linkedTechnicians = context.Technicians
            .Where(technician => technician.UserId == userId.Value);
        if (exceptTechnicianId.HasValue)
            linkedTechnicians = linkedTechnicians.Where(technician => technician.Id != exceptTechnicianId.Value);

        return !await linkedTechnicians.AnyAsync();
    }

    public Task<bool> UserHasTechnicianRoleAsync(int userId) => context.UserRoles
        .AnyAsync(link => link.UserId == userId && link.Role.IsActive && link.Role.Name == "Technician");

    public Task<bool> EmployeeCodeExistsAsync(string employeeCode, int? exceptTechnicianId = null)
    {
        var query = context.Technicians.Where(technician => technician.EmployeeCode == employeeCode);
        if (exceptTechnicianId.HasValue)
            query = query.Where(technician => technician.Id != exceptTechnicianId.Value);

        return query.AnyAsync();
    }

    public Task<bool> HasRepairTicketsAsync(int technicianId) =>
        context.RepairTickets.AnyAsync(ticket => ticket.TechnicianId == technicianId);

    public async Task AddAsync(Technician technician)
    {
        if (technician.UserId is int userId)
        {
            var user = await context.Users.FirstOrDefaultAsync(user => user.Id == userId);
            if (user is not null) user.TokenVersion++;
        }
        await context.Technicians.AddAsync(technician);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Technician technician)
    {
        var entry = context.Entry(technician);
        var previousUserId = entry.Property(x => x.UserId).OriginalValue;
        var previousIsActive = entry.Property(x => x.IsActive).OriginalValue;
        var accessChanged = previousUserId != technician.UserId || previousIsActive != technician.IsActive;
        if (accessChanged)
        {
            var affectedUserIds = new[] { previousUserId, technician.UserId }
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .Distinct()
                .ToArray();
            var users = await context.Users.Where(user => affectedUserIds.Contains(user.Id)).ToListAsync();
            foreach (var user in users) user.TokenVersion++;
        }
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Technician technician)
    {
        context.Technicians.Remove(technician);
        await context.SaveChangesAsync();
    }
}
