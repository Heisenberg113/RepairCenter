using RepairCenter.Entities;
using RepairCenter.Enums;
using RepairCenter.Contracts;

namespace RepairCenter.Services.Interfaces;

public interface IRepairTicketService
{
    Task<PagedRepairTicketResult<RepairTicket>> GetAllAsync(
        int page, int pageSize, int? customerId, int? deviceId, int? technicianId,
        int? technicianUserId, RepairStatus? status,
        DateTime? receivedFrom, DateTime? receivedTo, string? search, bool oldestFirst);
    Task<RepairTicket?> GetByIdAsync(int id);
    Task<RepairTicket?> GetByTicketNumberAsync(string ticketNumber);
    Task<List<RepairTicketStatusHistory>> GetStatusHistoryAsync(int repairTicketId);
    Task<RepairTicketOperationResult<RepairTicket>> CreateAsync(RepairTicket ticket);
    Task<RepairTicketOperationResult<RepairTicket>> AssignTechnicianAsync(int repairTicketId, int? technicianId);
    Task<RepairTicketOperationResult<RepairTicket>> ChangeStatusAsync(
        int repairTicketId,
        RepairStatus newStatus,
        int changedByUserId,
        string? note);
}
