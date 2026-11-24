using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RepairCenter.Data;
using RepairCenter.DTOs.RepairTickets;
using RepairCenter.Entities;
using RepairCenter.Enums;
using RepairCenter.Repositories.Interfaces;

namespace RepairCenter.Repositories;

public sealed class RepairQuoteRepository(RepairCenterDbContext db) : IRepairQuoteRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public Task<List<RepairQuoteVersion>> GetVersionsAsync(int ticketId) => db.RepairQuoteVersions.AsNoTracking()
        .Where(x => x.RepairTicketId == ticketId)
        .OrderBy(x => x.VersionNumber)
        .ToListAsync();

    public async Task<(RepairQuoteVersion? Version, RepairQuoteError Error, bool IsDuplicate)> CreateVersionAsync(int ticketId, int userId)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var ticket = await db.RepairTickets.AsNoTracking().FirstOrDefaultAsync(x => x.Id == ticketId);
        if (ticket is null) return (null, RepairQuoteError.TicketNotFound, false);
        if (IsTicketClosed(ticket.Status)) return (null, RepairQuoteError.TicketClosed, false);

        var details = await GetQuoteLinesAsync(ticketId);
        if (details.Count == 0) return (null, RepairQuoteError.NoDetails, false);
        var json = JsonSerializer.Serialize(details, JsonOptions);
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)));
        var existingVersion = await db.RepairQuoteVersions.AsNoTracking()
            .Where(x => x.RepairTicketId == ticketId && x.SnapshotHash == hash)
            .OrderByDescending(x => x.VersionNumber)
            .FirstOrDefaultAsync();
        if (existingVersion is not null)
        {
            await transaction.CommitAsync();
            return (existingVersion, RepairQuoteError.None, true);
        }

        var versionNumber = (await db.RepairQuoteVersions.Where(x => x.RepairTicketId == ticketId)
            .Select(x => (int?)x.VersionNumber).MaxAsync() ?? 0) + 1;
        var createdAt = DateTime.UtcNow;
        var version = new RepairQuoteVersion
        {
            RepairTicketId = ticketId,
            VersionNumber = versionNumber,
            SnapshotJson = json,
            SnapshotHash = hash,
            TotalAmount = details.Sum(x => x.TotalAmount),
            CreatedByUserId = userId,
            CreatedAt = createdAt
        };
        db.RepairQuoteVersions.Add(version);
        db.RepairTicketStatusHistories.Add(new RepairTicketStatusHistory
        {
            RepairTicketId = ticketId,
            OldStatus = ticket.Status,
            NewStatus = ticket.Status,
            ChangedByUserId = userId,
            ChangedAt = createdAt,
            Note = "Thay đổi báo giá"
        });
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return (version, RepairQuoteError.None, false);
    }

    public async Task<(RepairQuoteVersion? Version, RepairQuoteError Error)> RecordResponseAsync(
        int ticketId, int versionId, int userId, RecordQuoteResponseRequest request)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var ticketStatus = await db.RepairTickets.AsNoTracking()
            .Where(x => x.Id == ticketId)
            .Select(x => (RepairStatus?)x.Status)
            .FirstOrDefaultAsync();
        if (ticketStatus is null) return (null, RepairQuoteError.TicketNotFound);
        if (IsTicketClosed(ticketStatus.Value)) return (null, RepairQuoteError.TicketClosed);

        var version = await db.RepairQuoteVersions
            .FirstOrDefaultAsync(x => x.Id == versionId && x.RepairTicketId == ticketId);
        if (version is null) return (null, RepairQuoteError.VersionNotFound);
        version.Response = request.Response;
        version.ResponseChannel = request.Channel.Trim();
        version.ResponseNote = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();
        version.RespondedByUserId = userId;
        version.RespondedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return (version, RepairQuoteError.None);
    }

    public async Task<bool> CurrentQuoteAcceptedAsync(int ticketId)
    {
        var version = await db.RepairQuoteVersions.Where(x => x.RepairTicketId == ticketId)
            .OrderByDescending(x => x.VersionNumber).FirstOrDefaultAsync();
        if (version is null || version.Response != "Accepted") return false;
        var lines = await GetQuoteLinesAsync(ticketId);
        var json = JsonSerializer.Serialize(lines, JsonOptions);
        return CryptographicOperations.FixedTimeEquals(Convert.FromHexString(version.SnapshotHash),
            SHA256.HashData(Encoding.UTF8.GetBytes(json)));
    }

    private Task<List<QuoteLine>> GetQuoteLinesAsync(int ticketId) => db.RepairTicketDetails.AsNoTracking()
        .Where(x => x.RepairTicketId == ticketId)
        .OrderBy(x => x.Id)
        .Select(x => new QuoteLine(x.Id, x.RepairServiceId.HasValue ? "Service" : "SparePart",
            x.Description, x.Quantity, x.UnitPrice, x.DiscountPercent, x.DiscountAmount, x.TotalAmount))
        .ToListAsync();

    private static bool IsTicketClosed(RepairStatus status) =>
        status is RepairStatus.Completed or RepairStatus.Delivered or RepairStatus.Cancelled;

    private sealed record QuoteLine(int DetailId, string ItemType, string Description, decimal Quantity,
        decimal UnitPrice, decimal DiscountPercent, decimal DiscountAmount, decimal TotalAmount);
}
