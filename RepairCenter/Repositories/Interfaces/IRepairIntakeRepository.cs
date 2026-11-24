using RepairCenter.DTOs.RepairTickets;
using RepairCenter.Entities;

namespace RepairCenter.Repositories.Interfaces;

public enum RepairIntakeError { None, CustomerNotFound, DeviceNotFoundForCustomer, DeviceTypeNotFound }

public interface IRepairIntakeRepository
{
    Task<(RepairTicket? Ticket, RepairIntakeError Error)> CreateAsync(CreateRepairIntakeRequest request);
}
