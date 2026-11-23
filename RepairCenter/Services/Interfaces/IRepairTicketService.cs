using RepairCenter.Entities;
using RepairCenter.Enums;

namespace RepairCenter.Services.Interfaces;

public interface IRepairTicketService
{
    Task<List<RepairTicket>> GetAllAsync(int? customerId = null, int? deviceId = null);
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
