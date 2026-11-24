using RepairCenter.Entities;
using RepairCenter.Enums;
using RepairCenter.Repositories.Interfaces;
using RepairCenter.Services.Interfaces;

namespace RepairCenter.Services;

public class RepairTicketService(IRepairTicketRepository repository) : IRepairTicketService
{
    public Task<List<RepairTicket>> GetAllAsync(int? customerId = null, int? deviceId = null) =>
        repository.GetAllAsync(customerId, deviceId);

    public Task<RepairTicket?> GetByIdAsync(int id) => repository.GetByIdAsync(id);
    public Task<RepairTicket?> GetByTicketNumberAsync(string ticketNumber) => repository.GetByTicketNumberAsync(ticketNumber);

    public async Task<List<RepairTicketStatusHistory>> GetStatusHistoryAsync(int repairTicketId)
    {
        if (await repository.GetByIdAsync(repairTicketId) is null)
            return [];

        return await repository.GetStatusHistoryAsync(repairTicketId);
    }

    public async Task<RepairTicketOperationResult<RepairTicket>> AssignTechnicianAsync(
        int repairTicketId,
        int? technicianId)
    {
        var ticket = await repository.GetForUpdateAsync(repairTicketId);
        if (ticket is null)
            return RepairTicketOperationResult<RepairTicket>.Failure(RepairTicketOperationError.RepairTicketNotFound);

        if (ticket.Status is RepairStatus.Delivered or RepairStatus.Cancelled)
            return RepairTicketOperationResult<RepairTicket>.Failure(RepairTicketOperationError.TicketIsClosed);

        if (!technicianId.HasValue)
        {
            if (ticket.Status is RepairStatus.InProgress or RepairStatus.WaitingForParts or RepairStatus.Completed)
                return RepairTicketOperationResult<RepairTicket>.Failure(RepairTicketOperationError.CannotUnassignTechnicianDuringWork);

            ticket.TechnicianId = null;
            ticket.Technician = null;
            await repository.SaveChangesAsync(ticket);
            return RepairTicketOperationResult<RepairTicket>.Success(ticket);
        }

        var technician = await repository.GetActiveTechnicianAsync(technicianId.Value);
        if (technician is null)
            return RepairTicketOperationResult<RepairTicket>.Failure(RepairTicketOperationError.TechnicianNotFoundOrInactive);

        if (!technician.DeviceTypes.Any(link => link.DeviceTypeId == ticket.Device.DeviceTypeId))
            return RepairTicketOperationResult<RepairTicket>.Failure(RepairTicketOperationError.TechnicianNotQualifiedForDevice);

        ticket.TechnicianId = technician.Id;
        ticket.Technician = technician;
        await repository.SaveChangesAsync(ticket);
        return RepairTicketOperationResult<RepairTicket>.Success(ticket);
    }

    public async Task<RepairTicketOperationResult<RepairTicket>> ChangeStatusAsync(
        int repairTicketId,
        RepairStatus newStatus,
        int changedByUserId,
        string? note)
    {
        if (!Enum.IsDefined(newStatus))
            return RepairTicketOperationResult<RepairTicket>.Failure(RepairTicketOperationError.InvalidStatusTransition);

        var ticket = await repository.GetForUpdateAsync(repairTicketId);
        if (ticket is null)
            return RepairTicketOperationResult<RepairTicket>.Failure(RepairTicketOperationError.RepairTicketNotFound);

        if (!await repository.UserIsActiveAsync(changedByUserId))
            return RepairTicketOperationResult<RepairTicket>.Failure(RepairTicketOperationError.ChangedByUserNotFoundOrInactive);

        if (!IsAllowedTransition(ticket.Status, newStatus))
            return RepairTicketOperationResult<RepairTicket>.Failure(RepairTicketOperationError.InvalidStatusTransition);

        if ((newStatus is RepairStatus.InProgress or RepairStatus.WaitingForParts or RepairStatus.Completed) &&
            !ticket.TechnicianId.HasValue)
        {
            return RepairTicketOperationResult<RepairTicket>.Failure(RepairTicketOperationError.TechnicianRequiredForStatus);
        }

        var oldStatus = ticket.Status;
        ticket.Status = newStatus;
        if (newStatus == RepairStatus.Completed)
            ticket.CompletedAt = DateTime.UtcNow;
        if (newStatus == RepairStatus.Delivered)
            ticket.DeliveredAt = DateTime.UtcNow;

        var statusHistory = new RepairTicketStatusHistory
        {
            RepairTicketId = ticket.Id,
            OldStatus = oldStatus,
            NewStatus = newStatus,
            ChangedByUserId = changedByUserId,
            ChangedAt = DateTime.UtcNow,
            Note = NormalizeOptional(note)
        };

        await repository.SaveChangesAsync(ticket, statusHistory);
        return RepairTicketOperationResult<RepairTicket>.Success(ticket);
    }

    public async Task<RepairTicketOperationResult<RepairTicket>> CreateAsync(RepairTicket ticket)
    {
        if (!await repository.CustomerExistsAsync(ticket.CustomerId))
            return RepairTicketOperationResult<RepairTicket>.Failure(RepairTicketOperationError.CustomerNotFound);

        var device = await repository.GetDeviceForCustomerAsync(ticket.DeviceId, ticket.CustomerId);
        if (device is null)
            return RepairTicketOperationResult<RepairTicket>.Failure(RepairTicketOperationError.DeviceNotFoundForCustomer);

        ticket.Device = device;
        ticket.Customer = (await repository.GetCustomerAsync(ticket.CustomerId))!;
        ticket.TicketNumber = $"RC-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}"[..21].ToUpperInvariant();
        ticket.Status = RepairStatus.Received;
        ticket.ReceivedAt = DateTime.UtcNow;
        ticket.IssueDescription = ticket.IssueDescription.Trim();
        ticket.CustomerNotes = NormalizeOptional(ticket.CustomerNotes);

        await repository.AddAsync(ticket);
        // Return the persisted ticket with the same navigation graph used by API responses.
        var created = await repository.GetByIdAsync(ticket.Id);
        return created is null
            ? RepairTicketOperationResult<RepairTicket>.Success(ticket)
            : RepairTicketOperationResult<RepairTicket>.Success(created);
    }

    private static string? NormalizeOptional(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrEmpty(normalized) ? null : normalized;
    }

    private static bool IsAllowedTransition(RepairStatus current, RepairStatus next) => (current, next) switch
    {
        (RepairStatus.Received, RepairStatus.Diagnosing or RepairStatus.Cancelled) => true,
        (RepairStatus.Diagnosing, RepairStatus.WaitingForApproval or RepairStatus.InProgress or RepairStatus.Cancelled) => true,
        (RepairStatus.WaitingForApproval, RepairStatus.InProgress or RepairStatus.WaitingForParts or RepairStatus.Cancelled) => true,
        (RepairStatus.InProgress, RepairStatus.WaitingForApproval or RepairStatus.WaitingForParts or RepairStatus.Completed or RepairStatus.Cancelled) => true,
        (RepairStatus.WaitingForParts, RepairStatus.InProgress or RepairStatus.Cancelled) => true,
        (RepairStatus.Completed, RepairStatus.Delivered) => true,
        _ => false
    };
}
