using Microsoft.EntityFrameworkCore;
using RepairCenter.Data;
using RepairCenter.Entities;
using RepairCenter.Repositories.Interfaces;

namespace RepairCenter.Repositories;

public sealed class DeviceTypeRepository(RepairCenterDbContext db) : IDeviceTypeRepository
{
    public Task<List<DeviceType>> GetAllAsync() => db.DeviceTypes.AsNoTracking()
        .OrderBy(x => x.Name).ToListAsync();

    public Task<DeviceType?> GetByIdAsync(int id) => db.DeviceTypes
        .FirstOrDefaultAsync(x => x.Id == id);

    public Task<bool> NameExistsAsync(string name, int? exceptId = null)
    {
        var query = db.DeviceTypes.Where(x => x.Name == name);
        if (exceptId.HasValue) query = query.Where(x => x.Id != exceptId.Value);
        return query.AnyAsync();
    }

    public async Task<bool> IsInUseAsync(int id) =>
        await db.Devices.AnyAsync(x => x.DeviceTypeId == id) ||
        await db.TechnicianDeviceTypes.AnyAsync(x => x.DeviceTypeId == id) ||
        await db.RepairServices.AnyAsync(x => x.DeviceTypeId == id) ||
        await db.SpareParts.AnyAsync(x => x.DeviceTypeId == id);

    public async Task AddAsync(DeviceType deviceType)
    {
        await db.DeviceTypes.AddAsync(deviceType);
        await db.SaveChangesAsync();
    }

    public Task SaveAsync() => db.SaveChangesAsync();

    public async Task DeleteAsync(DeviceType deviceType)
    {
        db.DeviceTypes.Remove(deviceType);
        await db.SaveChangesAsync();
    }
}
