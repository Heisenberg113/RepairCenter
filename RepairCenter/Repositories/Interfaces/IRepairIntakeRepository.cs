using RepairCenter.DTOs.RepairTickets;
using RepairCenter.Entities;
using RepairCenter.Contracts;

namespace RepairCenter.Repositories.Interfaces;

public interface IRepairIntakeRepository
{
    Task<(RepairTicket? Ticket, RepairIntakeError Error)> CreateAsync(CreateRepairIntakeRequest request);
}
