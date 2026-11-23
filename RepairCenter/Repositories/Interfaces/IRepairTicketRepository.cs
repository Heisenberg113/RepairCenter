using RepairCenter.Entities;

namespace RepairCenter.Repositories.Interfaces;

public interface IRepairTicketRepository
{
    Task<List<RepairTicket>> GetAllAsync(int? customerId = null, int? deviceId = null);
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
