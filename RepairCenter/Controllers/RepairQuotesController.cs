using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RepairCenter.DTOs.RepairTickets;
using RepairCenter.Entities;
using RepairCenter.Contracts;
using RepairCenter.Services.Interfaces;

namespace RepairCenter.Controllers;

[ApiController]
[Route("api/repair-tickets/{ticketId:int}/quotes")]
[Authorize(Roles = "Admin,Receptionist,Technician")]
public class RepairQuotesController(IRepairTicketService tickets, IRepairQuoteService quotes) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<RepairQuoteVersionResponse>>> GetVersions(int ticketId)
    {
        if (!await CanAccessTicket(ticketId)) return NotFound();
        var versions = await quotes.GetVersionsAsync(ticketId);
        return Ok(versions.Select(version => ToResponse(version)));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Receptionist")]
    public async Task<ActionResult<RepairQuoteVersionResponse>> CreateVersion(int ticketId)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (!await CanAccessTicket(ticketId)) return NotFound();
        var (version, error, isDuplicate) = await quotes.CreateVersionAsync(ticketId, userId);
        if (error != RepairQuoteError.None) return ToError(error);
        return isDuplicate
            ? Ok(ToResponse(version!, isDuplicate: true))
            : CreatedAtAction(nameof(GetVersions), new { ticketId }, ToResponse(version!));
    }

    [HttpPost("{versionId:int}/response")]
    [Authorize(Roles = "Admin,Receptionist")]
    public async Task<ActionResult<RepairQuoteVersionResponse>> RecordResponse(
        int ticketId, int versionId, RecordQuoteResponseRequest request)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (!await CanAccessTicket(ticketId)) return NotFound();
        var (version, error) = await quotes.RecordResponseAsync(ticketId, versionId, userId, request);
        if (error != RepairQuoteError.None) return ToError(error);
        return Ok(ToResponse(version!));
    }

    private async Task<bool> CanAccessTicket(int ticketId)
    {
        var ticket = await tickets.GetByIdAsync(ticketId);
        if (ticket is null) return false;
        if (User.IsInRole("Admin") || User.IsInRole("Receptionist")) return true;
        return TryGetUserId(out var userId) && ticket.Technician?.UserId == userId;
    }

    private bool TryGetUserId(out int userId) =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);

    private ActionResult<RepairQuoteVersionResponse> ToError(RepairQuoteError error) => error switch
    {
        RepairQuoteError.TicketNotFound => NotFound(),
        RepairQuoteError.TicketClosed => Conflict(new ProblemDetails { Title = "Phiếu đã đóng, không thể ghi nhận báo giá." }),
        RepairQuoteError.NoDetails => Conflict(new ProblemDetails
        {
            Title = "Báo giá chưa có hạng mục",
            Detail = "Thêm ít nhất một dịch vụ hoặc phụ tùng trước khi gửi báo giá."
        }),
        RepairQuoteError.VersionNotFound => NotFound(),
        _ => Problem(statusCode: StatusCodes.Status500InternalServerError)
    };

    private static RepairQuoteVersionResponse ToResponse(RepairQuoteVersion version, bool isDuplicate = false) => new()
    {
        Id = version.Id,
        RepairTicketId = version.RepairTicketId,
        VersionNumber = version.VersionNumber,
        SnapshotJson = version.SnapshotJson,
        TotalAmount = version.TotalAmount,
        CreatedAt = DateTime.SpecifyKind(version.CreatedAt, DateTimeKind.Utc),
        Response = version.Response,
        RespondedAt = version.RespondedAt.HasValue
            ? DateTime.SpecifyKind(version.RespondedAt.Value, DateTimeKind.Utc)
            : null,
        ResponseChannel = version.ResponseChannel,
        ResponseNote = version.ResponseNote,
        IsDuplicate = isDuplicate
    };
}
