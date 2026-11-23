using Microsoft.EntityFrameworkCore;
using RepairCenter.Data;
using RepairCenter.Entities;
using RepairCenter.Repositories.Interfaces;

namespace RepairCenter.Repositories;

public sealed class RepairServiceRepository(RepairCenterDbContext db) : IRepairServiceRepository
{
    public Task<List<RepairService>> GetAllAsync(bool includeInactive)
    {
        IQueryable<RepairService> query = db.RepairServices.AsNoTracking().Include(x => x.DeviceType);
        if (!includeInactive) query = query.Where(x => x.IsActive);
        return query.OrderBy(x => x.Name).ToListAsync();
    }

    public Task<RepairService?> GetByIdAsync(int id) => db.RepairServices.Include(x => x.DeviceType).FirstOrDefaultAsync(x => x.Id == id);

    public Task<bool> DeviceTypeExistsAsync(int id) => db.DeviceTypes.AnyAsync(x => x.Id == id);

    public Task<bool> NameExistsAsync(string name, int? exceptId = null)
    {
        var query = db.RepairServices.Where(x => x.Name == name);
        if (exceptId.HasValue) query = query.Where(x => x.Id != exceptId.Value);
        return query.AnyAsync();
    }

    public Task<bool> IsInUseAsync(int id) => db.RepairTicketDetails.AnyAsync(x => x.RepairServiceId == id);

    public async Task AddAsync(RepairService service)
    {
        await db.RepairServices.AddAsync(service);
        await db.SaveChangesAsync();
    }

    public Task SaveAsync() => db.SaveChangesAsync();

    public async Task DeleteAsync(RepairService service)
    {
        db.RepairServices.Remove(service);
        await db.SaveChangesAsync();
    }
}
