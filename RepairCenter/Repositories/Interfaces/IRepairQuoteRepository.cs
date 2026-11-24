using RepairCenter.DTOs.RepairTickets;
using RepairCenter.Entities;

namespace RepairCenter.Repositories.Interfaces;

public enum RepairQuoteError { None, TicketNotFound, TicketClosed, NoDetails, VersionNotFound }

public interface IRepairQuoteRepository
{
    Task<List<RepairQuoteVersion>> GetVersionsAsync(int ticketId);
    Task<(RepairQuoteVersion? Version, RepairQuoteError Error, bool IsDuplicate)> CreateVersionAsync(int ticketId, int userId);
    Task<(RepairQuoteVersion? Version, RepairQuoteError Error)> RecordResponseAsync(
        int ticketId, int versionId, int userId, RecordQuoteResponseRequest request);
    Task<bool> CurrentQuoteAcceptedAsync(int ticketId);
}
