using RepairCenter.Entities;
using RepairCenter.Services.Interfaces;

namespace RepairCenter.Repositories.Interfaces;

public interface IRepairTicketRepository
{
    Task<PagedResult<RepairTicket>> GetAllAsync(
        int page, int pageSize, int? customerId, int? deviceId, int? technicianId,
        int? technicianUserId, RepairCenter.Enums.RepairStatus? status,
        DateTime? receivedFrom, DateTime? receivedTo, string? search);
    Task<RepairTicket?> GetByIdAsync(int id);
    Task<RepairTicket?> GetByTicketNumberAsync(string ticketNumber);
    Task<RepairTicket?> GetForUpdateAsync(int id);
    Task<List<RepairTicketStatusHistory>> GetStatusHistoryAsync(int repairTicketId);
    Task<bool> CustomerExistsAsync(int customerId);
    Task<Customer?> GetCustomerAsync(int customerId);
    Task<Device?> GetDeviceForCustomerAsync(int deviceId, int customerId);
    Task<Technician?> GetActiveTechnicianAsync(int technicianId);
    Task<bool> UserIsActiveAsync(int userId);
    Task AddAsync(RepairTicket ticket);
    Task SaveChangesAsync(RepairTicket ticket, RepairTicketStatusHistory? statusHistory = null);
}
