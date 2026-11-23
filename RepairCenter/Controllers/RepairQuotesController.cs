using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RepairCenter.Data;
using RepairCenter.DTOs.RepairTickets;
using RepairCenter.Entities;

namespace RepairCenter.Controllers;

[ApiController]
[Route("api/repair-tickets/{ticketId:int}/quotes")]
[Authorize(Roles = "Admin,Receptionist,Technician")]
public class RepairQuotesController(RepairCenterDbContext db) : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    [HttpGet]
    public async Task<ActionResult<IEnumerable<RepairQuoteVersionResponse>>> GetVersions(int ticketId)
    {
        if (!await CanAccessTicket(ticketId)) return NotFound();
        var versions = await db.RepairQuoteVersions.AsNoTracking().Where(x => x.RepairTicketId == ticketId)
            .OrderBy(x => x.VersionNumber).ToListAsync();
        return Ok(versions.Select(ToResponse));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Receptionist")]
    public async Task<ActionResult<RepairQuoteVersionResponse>> CreateVersion(int ticketId)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Unauthorized();
        var ticket = await db.RepairTickets.AsNoTracking().FirstOrDefaultAsync(x => x.Id == ticketId);
        if (ticket is null) return NotFound();
        if (ticket.Status is RepairCenter.Enums.RepairStatus.Delivered or RepairCenter.Enums.RepairStatus.Cancelled)
            return Conflict(new ProblemDetails { Title = "Phiếu đã đóng, không thể tạo báo giá." });

        var details = await db.RepairTicketDetails.AsNoTracking().Where(x => x.RepairTicketId == ticketId)
            .OrderBy(x => x.Id).Select(x => new QuoteLine(x.Id, x.RepairServiceId.HasValue ? "Service" : "SparePart", x.Description, x.Quantity,
                x.UnitPrice, x.DiscountPercent, x.DiscountAmount, x.TotalAmount)).ToListAsync();
        if (details.Count == 0)
            return Conflict(new ProblemDetails { Title = "Báo giá chưa có hạng mục", Detail = "Thêm ít nhất một dịch vụ hoặc phụ tùng trước khi gửi báo giá." });
        var json = JsonSerializer.Serialize(details, JsonOptions);
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)));
        var versionNumber = (await db.RepairQuoteVersions.Where(x => x.RepairTicketId == ticketId)
            .Select(x => (int?)x.VersionNumber).MaxAsync() ?? 0) + 1;
        var version = new RepairQuoteVersion
        {
            RepairTicketId = ticketId, VersionNumber = versionNumber, SnapshotJson = json,
            SnapshotHash = hash, TotalAmount = details.Sum(x => x.TotalAmount), CreatedByUserId = userId
        };
        db.RepairQuoteVersions.Add(version);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetVersions), new { ticketId }, ToResponse(version));
    }

    [HttpPost("{versionId:int}/response")]
    [Authorize(Roles = "Admin,Receptionist")]
    public async Task<ActionResult<RepairQuoteVersionResponse>> RecordResponse(int ticketId, int versionId, RecordQuoteResponseRequest request)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Unauthorized();
        var version = await db.RepairQuoteVersions.FirstOrDefaultAsync(x => x.Id == versionId && x.RepairTicketId == ticketId);
        if (version is null) return NotFound();
        version.Response = request.Response;
        version.ResponseChannel = request.Channel.Trim();
        version.ResponseNote = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();
        version.RespondedByUserId = userId;
        version.RespondedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Ok(ToResponse(version));
    }

    internal static async Task<bool> CurrentQuoteAcceptedAsync(RepairCenterDbContext db, int ticketId)
    {
        var version = await db.RepairQuoteVersions.Where(x => x.RepairTicketId == ticketId)
            .OrderByDescending(x => x.VersionNumber).FirstOrDefaultAsync();
        if (version is null || version.Response != "Accepted") return false;
        var lines = await db.RepairTicketDetails.AsNoTracking().Where(x => x.RepairTicketId == ticketId)
            .OrderBy(x => x.Id).Select(x => new QuoteLine(x.Id, x.RepairServiceId.HasValue ? "Service" : "SparePart", x.Description, x.Quantity,
                x.UnitPrice, x.DiscountPercent, x.DiscountAmount, x.TotalAmount)).ToListAsync();
        var json = JsonSerializer.Serialize(lines, JsonOptions);
        return CryptographicOperations.FixedTimeEquals(Convert.FromHexString(version.SnapshotHash),
            SHA256.HashData(Encoding.UTF8.GetBytes(json)));
    }

    private async Task<bool> CanAccessTicket(int ticketId)
    {
        if (User.IsInRole("Admin") || User.IsInRole("Receptionist")) return await db.RepairTickets.AnyAsync(x => x.Id == ticketId);
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return false;
        return await db.RepairTickets.AnyAsync(x => x.Id == ticketId && x.Technician != null && x.Technician.UserId == userId);
    }

    private static RepairQuoteVersionResponse ToResponse(RepairQuoteVersion x) => new()
    {
        Id = x.Id, RepairTicketId = x.RepairTicketId, VersionNumber = x.VersionNumber,
        SnapshotJson = x.SnapshotJson, TotalAmount = x.TotalAmount, CreatedAt = x.CreatedAt,
        Response = x.Response, RespondedAt = x.RespondedAt, ResponseChannel = x.ResponseChannel, ResponseNote = x.ResponseNote
    };

    private sealed record QuoteLine(int DetailId, string ItemType, string Description, decimal Quantity,
        decimal UnitPrice, decimal DiscountPercent, decimal DiscountAmount, decimal TotalAmount);
}
