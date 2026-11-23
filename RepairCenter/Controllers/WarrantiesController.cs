using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using RepairCenter.DTOs.Warranties;
using RepairCenter.Entities;
using RepairCenter.Enums;
using RepairCenter.Repositories.Interfaces;
using RepairCenter.Services.Interfaces;
using System.Security.Claims;

namespace RepairCenter.Controllers;

[ApiController]
[Route("api/warranties")]
[Authorize]
public class WarrantiesController(IBillingService service, IWarrantyWorkflowService workflow, IRepairTicketService tickets) : ControllerBase
{
    [HttpGet("by-ticket/{ticketId:int}")]
    [Authorize(Roles = "Admin,Receptionist,Technician")]
    public async Task<ActionResult<WarrantyResponse>> GetByTicket(int ticketId)
    {
        var ticket = await tickets.GetByIdAsync(ticketId);
        if (ticket is null) return NotFound();
        if (User.IsInRole("Technician") && !User.IsInRole("Admin") && !User.IsInRole("Receptionist") &&
            (!TryGetUserId(out var userId) || ticket.Technician?.UserId != userId || ticket.Technician.IsActive == false))
            return NotFound();
        var warranty = await service.GetWarrantyByTicketAsync(ticketId);
        return warranty is null ? NotFound() : Ok(ToResponse(warranty));
    }

    [HttpGet("{code}")]
    [Authorize(Roles = "Admin,Receptionist")]
    public async Task<ActionResult<WarrantyResponse>> GetByCode(string code)
    {
        var warranty = await service.GetWarrantyByCodeAsync(code);
        return warranty is null ? NotFound() : Ok(ToResponse(warranty));
    }

    [HttpPost("by-ticket/{ticketId:int}")]
    [Authorize(Roles = "Admin,Receptionist")]
    public async Task<ActionResult<WarrantyResponse>> Issue(int ticketId, IssueWarrantyRequest request)
    {
        var result = await service.IssueWarrantyAsync(ticketId);
        if (result.Error != BillingError.None)
        {
            return result.Error switch
            {
                BillingError.TicketNotFound => NotFound(new ProblemDetails { Title = "Không tìm thấy phiếu sửa chữa." }),
                BillingError.TicketNotDelivered => Conflict(new ProblemDetails { Title = "Chỉ cấp bảo hành sau khi phiếu đã giao khách." }),
                BillingError.WarrantyAlreadyExists => Conflict(new ProblemDetails { Title = "Phiếu đã được cấp bảo hành." }),
                BillingError.NoWarrantyCoverage => Conflict(new ProblemDetails { Title = "Phiếu chưa có hạng mục nào được cấu hình thời hạn bảo hành." }),
                BillingError.InvalidWarrantyPolicy => Conflict(new ProblemDetails { Title = "Có hạng mục chứa chính sách bảo hành không hợp lệ." }),
                _ => Problem(statusCode: StatusCodes.Status500InternalServerError)
            };
        }
        var warranty = result.Value!;
        return CreatedAtAction(nameof(GetByCode), new { code = warranty.WarrantyCode }, ToResponse(warranty));
    }

    [HttpGet("{id:int}/claims")]
    [Authorize(Roles = "Admin,Receptionist")]
    public async Task<ActionResult<IEnumerable<WarrantyClaimResponse>>> GetClaims(int id)
    {
        if (await workflow.GetWarrantyAsync(id) is null) return NotFound();
        var claims = await workflow.GetClaimsAsync(id);
        return Ok(claims.Select(ToClaimResponse));
    }

    [HttpGet("claims/{claimId:int}")]
    [Authorize(Roles = "Admin,Receptionist,Technician")]
    public async Task<ActionResult<WarrantyClaimResponse>> GetClaim(int claimId)
    {
        var claim = await workflow.GetClaimAsync(claimId);
        if (claim is null) return NotFound();
        var canManage = User.IsInRole("Admin") || User.IsInRole("Receptionist");
        if (!canManage && (!TryGetUserId(out var userId) || claim.AssignedTechnician?.UserId != userId))
            return Forbid();
        return Ok(ToClaimResponse(claim));
    }

    [HttpGet("claims/pending")]
    [Authorize(Roles = "Admin,Receptionist,Technician")]
    public async Task<ActionResult<IEnumerable<WarrantyClaimResponse>>> GetPendingClaims()
    {
        List<WarrantyClaim> claims;
        if (User.IsInRole("Admin") || User.IsInRole("Receptionist"))
            claims = await workflow.GetPendingClaimsAsync();
        else if (TryGetUserId(out var userId))
            claims = await workflow.GetClaimsForTechnicianAsync(userId);
        else
            return Unauthorized();
        return Ok(claims.Select(ToClaimResponse));
    }

    [HttpPost("{id:int}/claims")]
    [Authorize(Roles = "Admin,Receptionist")]
    public async Task<ActionResult<WarrantyClaimResponse>> SubmitClaim(int id, CreateWarrantyClaimRequest request)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var warranty = await workflow.GetWarrantyAsync(id);
        if (warranty is null) return NotFound();
        var result = await workflow.SubmitClaimAsync(id, request.RepairTicketDetailId, userId, request.Description);
        if (result.Error != WarrantyWorkflowError.None) return WorkflowError(result.Error);
        return CreatedAtAction(nameof(GetClaim), new { claimId = result.Value!.Id }, ToClaimResponse(result.Value));
    }

    [HttpPost("claims/{claimId:int}/approve")]
    [Authorize(Roles = "Admin,Technician")]
    public async Task<ActionResult<WarrantyClaimResponse>> ApproveClaim(int claimId, ResolveWarrantyClaimRequest request) =>
        await ResolveClaim(claimId, request.Note, WarrantyClaimStatus.Approved);

    [HttpPost("claims/{claimId:int}/reject")]
    [Authorize(Roles = "Admin,Technician")]
    public async Task<ActionResult<WarrantyClaimResponse>> RejectClaim(int claimId, ResolveWarrantyClaimRequest request) =>
        await ResolveClaim(claimId, request.Note, WarrantyClaimStatus.Rejected);

    [HttpPost("claims/{claimId:int}/accept")]
    [Authorize(Roles = "Technician")]
    public async Task<IActionResult> AcceptClaim(int claimId)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var error = await workflow.AcceptClaimAsync(claimId, userId);
        return error == WarrantyWorkflowError.None ? NoContent() : WorkflowError(error);
    }

    [HttpPut("claims/{claimId:int}/assignment")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AssignClaim(int claimId, AssignWarrantyClaimRequest request)
    {
        var error = await workflow.AssignClaimAsync(claimId, request.TechnicianId);
        return error == WarrantyWorkflowError.None ? NoContent() : WorkflowError(error);
    }

    [HttpPost("claims/{claimId:int}/cancel")]
    [Authorize(Roles = "Admin,Receptionist,Technician")]
    public async Task<IActionResult> CancelClaim(int claimId)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var canManage = User.IsInRole("Admin") || User.IsInRole("Receptionist");
        var error = await workflow.CancelClaimAsync(claimId, userId, canManage);
        return error == WarrantyWorkflowError.None ? NoContent() : WorkflowError(error);
    }

    [HttpPost("{id:int}/void")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Void(int id, VoidWarrantyRequest request)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var error = await workflow.VoidWarrantyAsync(id, userId, request.Reason);
        if (error != WarrantyWorkflowError.None) return WorkflowError(error);
        return Ok(ToResponse((await workflow.GetWarrantyAsync(id))!));
    }

    private async Task<ActionResult<WarrantyClaimResponse>> ResolveClaim(int claimId, string note, WarrantyClaimStatus status)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await workflow.ResolveClaimAsync(claimId, userId, status, note, User.IsInRole("Admin"));
        return result.Error == WarrantyWorkflowError.None
            ? Ok(ToClaimResponse(result.Value!)) : WorkflowError(result.Error);
    }

    private ActionResult WorkflowError(WarrantyWorkflowError error) => error switch
    {
        WarrantyWorkflowError.WarrantyNotFound or WarrantyWorkflowError.ClaimNotFound or WarrantyWorkflowError.ItemNotFound => NotFound(),
        WarrantyWorkflowError.NotClaimOwner => Forbid(),
        WarrantyWorkflowError.TechnicianNotQualified => Forbid(),
        WarrantyWorkflowError.ClaimAlreadyAssigned => Conflict(new ProblemDetails { Title = "Yêu cầu đã được kỹ thuật viên khác nhận xử lý." }),
        WarrantyWorkflowError.InvalidActor => Unauthorized(),
        WarrantyWorkflowError.InvalidRequest => BadRequest(),
        WarrantyWorkflowError.WarrantyExpired => Conflict(new ProblemDetails { Title = "Bảo hành đã hết hạn." }),
        WarrantyWorkflowError.ItemWarrantyExpired => Conflict(new ProblemDetails { Title = "Hạng mục này đã hết hạn bảo hành." }),
        WarrantyWorkflowError.ItemNotCovered => Conflict(new ProblemDetails { Title = "Hạng mục này không có chính sách bảo hành." }),
        WarrantyWorkflowError.WarrantyNotActive => Conflict(new ProblemDetails { Title = "Bảo hành không ở trạng thái có thể xử lý." }),
        WarrantyWorkflowError.PendingClaimExists => Conflict(new ProblemDetails { Title = "Hạng mục này đã có yêu cầu đang chờ xử lý." }),
        WarrantyWorkflowError.ClaimNotPending => Conflict(new ProblemDetails { Title = "Yêu cầu bảo hành không còn ở trạng thái chờ." }),
        _ => Problem(statusCode: StatusCodes.Status500InternalServerError)
    };

    private bool TryGetUserId(out int userId) => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);

    private static WarrantyClaimResponse ToClaimResponse(WarrantyClaim claim) => new()
    {
        Id = claim.Id, WarrantyId = claim.WarrantyId, WarrantyCode = claim.Warranty.WarrantyCode,
        TicketNumber = claim.Warranty.RepairTicket.TicketNumber,
        CustomerName = claim.Warranty.RepairTicket.Customer.Name,
        RequestedByUserId = claim.RequestedByUserId, RequestedByUserName = claim.RequestedByUser.FullName,
        ResolvedByUserId = claim.ResolvedByUserId, ResolvedByUserName = claim.ResolvedByUser?.FullName,
        AssignedTechnicianId = claim.AssignedTechnicianId,
        AssignedTechnicianUserId = claim.AssignedTechnician?.UserId,
        AssignedTechnicianName = claim.AssignedTechnician?.FullName,
        AssignedAt = claim.AssignedAt,
        DeviceTypeId = claim.Warranty.RepairTicket.Device.DeviceTypeId,
        RepairTicketDetailId = claim.RepairTicketDetailId,
        ItemDescription = claim.RepairTicketDetail?.Description,
        GeneratedRepairTicketId = claim.GeneratedRepairTicketId,
        GeneratedRepairTicketNumber = claim.GeneratedRepairTicket?.TicketNumber,
        Description = claim.Description, ResolutionNote = claim.ResolutionNote,
        Status = claim.Status, SubmittedAt = claim.SubmittedAt, ResolvedAt = claim.ResolvedAt
    };

    private static WarrantyResponse ToResponse(Warranty warranty) => new()
    {
        Id = warranty.Id, WarrantyCode = warranty.WarrantyCode,
        RepairTicketId = warranty.RepairTicketId, TicketNumber = warranty.RepairTicket.TicketNumber,
        CustomerName = warranty.RepairTicket.Customer.Name,
        CustomerPhoneNumber = warranty.RepairTicket.Customer.PhoneNumber,
        StartDate = warranty.StartDate, EndDate = warranty.EndDate,
        Status = warranty.Status,
        Terms = warranty.Terms, VoidReason = warranty.VoidReason,
        VoidedAt = warranty.VoidedAt, VoidedByUserId = warranty.VoidedByUserId,
        Items = warranty.RepairTicket.Details.Select(detail => new WarrantyItemResponse
        {
            RepairTicketDetailId = detail.Id,
            ItemType = detail.RepairServiceId.HasValue ? "Service" : "SparePart",
            Description = detail.Description,
            WarrantyDuration = detail.WarrantyDuration,
            WarrantyDurationUnit = detail.WarrantyDurationUnit,
            StartDate = detail.WarrantyStartDate,
            EndDate = detail.WarrantyEndDate,
            IsInWarranty = warranty.Status == WarrantyStatus.Active && detail.WarrantyStartDate.HasValue &&
                detail.WarrantyEndDate.HasValue && detail.WarrantyStartDate.Value.Date <= DateTime.UtcNow.Date &&
                detail.WarrantyEndDate.Value.Date >= DateTime.UtcNow.Date,
            Terms = detail.WarrantyTerms
        }).ToList()
    };
}
