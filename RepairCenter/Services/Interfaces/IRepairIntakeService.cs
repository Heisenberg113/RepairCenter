using RepairCenter.DTOs.RepairTickets;
using RepairCenter.Entities;
using RepairCenter.Contracts;

namespace RepairCenter.Services.Interfaces;

public interface IRepairIntakeService
{
    Task<(RepairTicket? Ticket, RepairIntakeError Error)> CreateAsync(CreateRepairIntakeRequest request);
}
