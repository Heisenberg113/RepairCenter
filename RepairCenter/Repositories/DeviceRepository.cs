using Microsoft.EntityFrameworkCore;
using RepairCenter.Data;
using RepairCenter.Entities;
using RepairCenter.Repositories.Interfaces;

namespace RepairCenter.Repositories;

public class DeviceRepository(RepairCenterDbContext context) : IDeviceRepository
{
    public async Task<List<Device>> GetAllAsync(int? customerId = null, int? deviceTypeId = null)
    {
        var query = context.Devices
            .AsNoTracking()
            .Include(device => device.Customer)
            .Include(device => device.DeviceType)
            .AsQueryable();

        if (customerId.HasValue)
            query = query.Where(device => device.CustomerId == customerId.Value);

        if (deviceTypeId.HasValue)
            query = query.Where(device => device.DeviceTypeId == deviceTypeId.Value);

        return await query.OrderBy(device => device.Id).ToListAsync();
    }

    public Task<Device?> GetByIdAsync(int id) => context.Devices
        .Include(device => device.Customer)
        .Include(device => device.DeviceType)
        .FirstOrDefaultAsync(device => device.Id == id);

    public Task<Customer?> GetCustomerAsync(int customerId) =>
        context.Customers.FirstOrDefaultAsync(customer => customer.Id == customerId);

    public Task<DeviceType?> GetDeviceTypeAsync(int deviceTypeId) =>
        context.DeviceTypes.FirstOrDefaultAsync(type => type.Id == deviceTypeId);

    public Task<bool> HasRepairTicketsAsync(int deviceId) =>
        context.RepairTickets.AnyAsync(ticket => ticket.DeviceId == deviceId);

    public async Task AddAsync(Device device)
    {
        await context.Devices.AddAsync(device);
        await context.SaveChangesAsync();
    }

    public Task UpdateAsync(Device device) => context.SaveChangesAsync();

    public async Task DeleteAsync(Device device)
    {
        context.Devices.Remove(device);
        await context.SaveChangesAsync();
    }
}
