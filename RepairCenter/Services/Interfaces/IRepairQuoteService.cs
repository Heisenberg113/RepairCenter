using RepairCenter.DTOs.RepairTickets;
using RepairCenter.Entities;
using RepairCenter.Contracts;

namespace RepairCenter.Services.Interfaces;

public interface IRepairQuoteService
{
    Task<List<RepairQuoteVersion>> GetVersionsAsync(int ticketId);
    Task<(RepairQuoteVersion? Version, RepairQuoteError Error, bool IsDuplicate)> CreateVersionAsync(int ticketId, int userId);
    Task<(RepairQuoteVersion? Version, RepairQuoteError Error)> RecordResponseAsync(
        int ticketId, int versionId, int userId, RecordQuoteResponseRequest request);
    Task<bool> CurrentQuoteAcceptedAsync(int ticketId);
}
