using Microsoft.EntityFrameworkCore;
using System.Data;
using RepairCenter.Data;
using RepairCenter.Entities;
using RepairCenter.Repositories.Interfaces;
using RepairCenter.Contracts;

namespace RepairCenter.Repositories;

public class RepairTicketRepository(RepairCenterDbContext context) : IRepairTicketRepository
{
    public async Task<PagedRepairTicketResult<RepairTicket>> GetAllAsync(
        int page, int pageSize, int? customerId, int? deviceId, int? technicianId,
        int? technicianUserId, RepairCenter.Enums.RepairStatus? status,
        DateTime? receivedFrom, DateTime? receivedTo, string? search, bool oldestFirst)
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

        if (technicianId.HasValue)
            query = query.Where(ticket => ticket.TechnicianId == technicianId.Value);

        if (technicianUserId.HasValue)
            query = query.Where(ticket => ticket.Technician != null && ticket.Technician.UserId == technicianUserId.Value);

        if (status.HasValue)
            query = query.Where(ticket => ticket.Status == status.Value);

        if (receivedFrom.HasValue)
            query = query.Where(ticket => ticket.ReceivedAt >= receivedFrom.Value);

        if (receivedTo.HasValue)
            query = query.Where(ticket => ticket.ReceivedAt < receivedTo.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(ticket =>
                ticket.TicketNumber.Contains(term) ||
                ticket.Customer.Name.Contains(term) ||
                (ticket.Customer.PhoneNumber != null && ticket.Customer.PhoneNumber.Contains(term)) ||
                ticket.Device.Brand.Contains(term) ||
                ticket.Device.Model.Contains(term) ||
                ticket.IssueDescription.Contains(term));
        }

        var totalCount = await query.CountAsync();
        var activeCount = await query.CountAsync(ticket =>
            ticket.Status != RepairCenter.Enums.RepairStatus.Delivered &&
            ticket.Status != RepairCenter.Enums.RepairStatus.Cancelled);
        var ordered = oldestFirst
            ? query.OrderBy(ticket => ticket.ReceivedAt).ThenBy(ticket => ticket.Id)
            : query.OrderByDescending(ticket => ticket.ReceivedAt).ThenByDescending(ticket => ticket.Id);
        var items = await ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        return new PagedRepairTicketResult<RepairTicket>(items, page, pageSize, totalCount, activeCount);
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

    public Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction> BeginSerializableTransactionAsync() =>
        context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

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
