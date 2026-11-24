using RepairCenter.DTOs.RepairTickets;
using RepairCenter.Entities;
using RepairCenter.Contracts;
using RepairCenter.Repositories.Interfaces;
using RepairCenter.Services.Interfaces;

namespace RepairCenter.Services;

public sealed class RepairIntakeService(IRepairIntakeRepository repository) : IRepairIntakeService
{
    public Task<(RepairTicket? Ticket, RepairIntakeError Error)> CreateAsync(CreateRepairIntakeRequest request) =>
        repository.CreateAsync(request);
}
