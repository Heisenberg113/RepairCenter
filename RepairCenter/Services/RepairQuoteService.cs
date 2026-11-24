using RepairCenter.DTOs.RepairTickets;
using RepairCenter.Entities;
using RepairCenter.Contracts;
using RepairCenter.Repositories.Interfaces;
using RepairCenter.Services.Interfaces;

namespace RepairCenter.Services;

public sealed class RepairQuoteService(IRepairQuoteRepository repository) : IRepairQuoteService
{
    public Task<List<RepairQuoteVersion>> GetVersionsAsync(int ticketId) => repository.GetVersionsAsync(ticketId);

    public Task<(RepairQuoteVersion? Version, RepairQuoteError Error, bool IsDuplicate)> CreateVersionAsync(int ticketId, int userId) =>
        repository.CreateVersionAsync(ticketId, userId);

    public Task<(RepairQuoteVersion? Version, RepairQuoteError Error)> RecordResponseAsync(
        int ticketId, int versionId, int userId, RecordQuoteResponseRequest request) =>
        repository.RecordResponseAsync(ticketId, versionId, userId, request);

    public Task<bool> CurrentQuoteAcceptedAsync(int ticketId) => repository.CurrentQuoteAcceptedAsync(ticketId);
}
