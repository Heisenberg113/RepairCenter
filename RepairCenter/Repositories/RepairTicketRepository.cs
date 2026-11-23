using Microsoft.EntityFrameworkCore;
using RepairCenter.Data;
using RepairCenter.Entities;
using RepairCenter.Repositories.Interfaces;

namespace RepairCenter.Repositories;

public class RepairTicketRepository(RepairCenterDbContext context) : IRepairTicketRepository
{
    public Task<List<RepairTicket>> GetAllAsync(int? customerId = null, int? deviceId = null)
    {
        var query = context.RepairTickets
            .AsNoTracking()
            .Include(ticket => ticket.Customer)
            .Include(ticket => ticket.Device)
                .ThenInclude(device => device.DeviceType)
            .Include(ticket => ticket.Technician)
            .AsQueryable();

        if (customerId.HasValue)
            query = query.Where(ticket => ticket.CustomerId == customerId.Value);

        if (deviceId.HasValue)
            query = query.Where(ticket => ticket.DeviceId == deviceId.Value);

        return query.OrderByDescending(ticket => ticket.ReceivedAt).ToListAsync();
    }

    public Task<RepairTicket?> GetByIdAsync(int id) => context.RepairTickets
        .AsNoTracking()
        .Include(ticket => ticket.ParentRepairTicket)
        .Include(ticket => ticket.Customer)
        .Include(ticket => ticket.Device)
            .ThenInclude(device => device.DeviceType)
        .Include(ticket => ticket.Technician)
        .FirstOrDefaultAsync(ticket => ticket.Id == id);

    public Task<RepairTicket?> GetByTicketNumberAsync(string ticketNumber) => context.RepairTickets
        .AsNoTracking()
        .Include(ticket => ticket.ParentRepairTicket)
        .Include(ticket => ticket.Customer)
        .Include(ticket => ticket.Device)
            .ThenInclude(device => device.DeviceType)
        .Include(ticket => ticket.Technician)
        .FirstOrDefaultAsync(ticket => ticket.TicketNumber == ticketNumber.Trim());

    public Task<RepairTicket?> GetForUpdateAsync(int id) => context.RepairTickets
        .Include(ticket => ticket.Customer)
        .Include(ticket => ticket.Device)
            .ThenInclude(device => device.DeviceType)
        .Include(ticket => ticket.Technician)
        .FirstOrDefaultAsync(ticket => ticket.Id == id);

    public Task<List<RepairTicketStatusHistory>> GetStatusHistoryAsync(int repairTicketId) =>
        context.RepairTicketStatusHistories
            .AsNoTracking()
            .Include(history => history.ChangedByUser)
            .Where(history => history.RepairTicketId == repairTicketId)
            .OrderBy(history => history.ChangedAt)
            .ThenBy(history => history.Id)
            .ToListAsync();

    public Task<bool> CustomerExistsAsync(int customerId) =>
        context.Customers.AnyAsync(customer => customer.Id == customerId);

    public Task<Customer?> GetCustomerAsync(int customerId) =>
        context.Customers.FirstOrDefaultAsync(customer => customer.Id == customerId);

    public Task<Device?> GetDeviceForCustomerAsync(int deviceId, int customerId) =>
        context.Devices
            .Include(device => device.DeviceType)
            .FirstOrDefaultAsync(device => device.Id == deviceId && device.CustomerId == customerId);

    public Task<Technician?> GetActiveTechnicianAsync(int technicianId) => context.Technicians
        .Include(technician => technician.DeviceTypes)
        .FirstOrDefaultAsync(technician => technician.Id == technicianId && technician.IsActive);

    public Task<bool> UserIsActiveAsync(int userId) =>
        context.Users.AnyAsync(user => user.Id == userId && user.IsActive);

    public async Task AddAsync(RepairTicket ticket)
    {
        await context.RepairTickets.AddAsync(ticket);
        await context.SaveChangesAsync();
    }

    public async Task SaveChangesAsync(RepairTicket ticket, RepairTicketStatusHistory? statusHistory = null)
    {
        if (statusHistory is not null)
            await context.RepairTicketStatusHistories.AddAsync(statusHistory);

        await context.SaveChangesAsync();
    }
}
