using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using RepairCenter.DTOs.RepairTickets;
using RepairCenter.Entities;
using RepairCenter.Enums;
using RepairCenter.Services.Interfaces;
using RepairCenter.Data;

namespace RepairCenter.Controllers;

[ApiController]
[Route("api/repair-tickets")]
[Authorize(Roles = "Admin,Receptionist,Technician")]
public class RepairTicketsController(IRepairTicketService service, IRepairTicketDetailService detailService, RepairCenterDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<RepairTicketResponse>>> GetAll(
        [FromQuery] int? customerId,
        [FromQuery] int? deviceId)
    {
        var tickets = await service.GetAllAsync(customerId, deviceId);
        if (User.IsInRole("Technician") && !User.IsInRole("Admin"))
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return Ok(Array.Empty<RepairTicketResponse>());
            tickets = tickets.Where(ticket => ticket.Technician?.UserId == userId).ToList();
        }
        return Ok(tickets.Select(ToResponse));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<RepairTicketResponse>> GetById(int id)
    {
        var ticket = await service.GetByIdAsync(id);
        if (ticket is not null && !CanAccess(ticket)) return NotFound();
        return ticket is null ? NotFound() : Ok(ToResponse(ticket));
    }

    [HttpGet("by-number/{ticketNumber}")]
    public async Task<ActionResult<RepairTicketResponse>> GetByTicketNumber(string ticketNumber)
    {
        var ticket = await service.GetByTicketNumberAsync(ticketNumber);
        if (ticket is null || !CanAccess(ticket)) return NotFound();
        return Ok(ToResponse(ticket));
    }

    [HttpPut("{id:int}/technician")]
    [Authorize(Roles = "Admin,Receptionist")]
    public async Task<ActionResult<RepairTicketResponse>> AssignTechnician(
        int id,
        AssignTechnicianRequest request)
    {
        var result = await service.AssignTechnicianAsync(id, request.TechnicianId);
        if (result.Error != RepairTicketOperationError.None)
            return ToError(result.Error);

        return Ok(ToResponse(result.Value!));
    }

    [HttpDelete("{id:int}/technician")]
    [Authorize(Roles = "Admin,Receptionist")]
    public async Task<ActionResult<RepairTicketResponse>> UnassignTechnician(int id)
    {
        var result = await service.AssignTechnicianAsync(id, null);
        if (result.Error != RepairTicketOperationError.None)
            return ToError(result.Error);

        return Ok(ToResponse(result.Value!));
    }

    [HttpPatch("{id:int}/status")]
    public async Task<ActionResult<RepairTicketResponse>> ChangeStatus(
        int id,
        ChangeRepairTicketStatusRequest request)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var changedByUserId))
            return Unauthorized();
        var ticket = await service.GetByIdAsync(id);
        if (ticket is null || !CanAccess(ticket)) return NotFound();

        var nextStatus = request.NewStatus!.Value;
        if (!CanChangeStatus(ticket.Status, nextStatus))
            return Forbid();
        if (nextStatus == RepairStatus.InProgress &&
            !await RepairQuotesController.CurrentQuoteAcceptedAsync(db, id))
            return Conflict(new ProblemDetails
            {
                Title = "Báo giá chưa được khách đồng ý",
                Detail = "Cần tạo phiên bản báo giá hiện tại và ghi nhận khách đồng ý trước khi bắt đầu sửa."
            });

        var result = await service.ChangeStatusAsync(
            id,
            nextStatus,
            changedByUserId,
            request.Note);
        if (result.Error != RepairTicketOperationError.None)
            return ToError(result.Error);

        return Ok(ToResponse(result.Value!));
    }

    [HttpGet("{id:int}/status-history")]
    public async Task<ActionResult<IEnumerable<RepairTicketStatusHistoryResponse>>> GetStatusHistory(int id)
    {
        var ticket = await service.GetByIdAsync(id);
        if (ticket is null || !CanAccess(ticket))
            return NotFound();

        var history = await service.GetStatusHistoryAsync(id);
        return Ok(history.Select(entry => new RepairTicketStatusHistoryResponse
        {
            Id = entry.Id,
            OldStatus = entry.OldStatus,
            NewStatus = entry.NewStatus,
            ChangedByUserId = entry.ChangedByUserId,
            ChangedByUserName = entry.ChangedByUser.FullName,
            ChangedAt = DateTime.SpecifyKind(entry.ChangedAt, DateTimeKind.Utc),
            Note = entry.Note
        }));
    }

    [HttpGet("{id:int}/details")]
    public async Task<ActionResult<IEnumerable<RepairTicketDetailResponse>>> GetDetails(int id)
    {
        var ticket = await service.GetByIdAsync(id);
        if (ticket is null || !CanAccess(ticket)) return NotFound();
        var result = await detailService.GetByTicketAsync(id);
        if (result.Error != RepairTicketDetailError.None)
            return DetailError(result.Error);

        return Ok(result.Value!.Select(ToDetailResponse));
    }

    [HttpPut("{id:int}/details/{detailId:int}")]
    public async Task<IActionResult> UpdateDetail(int id, int detailId, UpdateRepairTicketDetailRequest request)
    {
        var ticket = await service.GetByIdAsync(id);
        if (ticket is null || !CanAccess(ticket)) return NotFound();
        var error = await detailService.UpdateAsync(id, detailId, request.Quantity, request.DiscountPercent);
        if (error == RepairTicketDetailError.None && !await ReturnToApprovalIfNeeded(ticket, "Hạng mục thay đổi khi đang sửa; chờ khách duyệt lại báo giá."))
            return Conflict(new ProblemDetails { Title = "Không thể chuyển phiếu về chờ duyệt." });
        return error == RepairTicketDetailError.None ? NoContent() : DetailError(error);
    }

    [HttpDelete("{id:int}/details/{detailId:int}")]
    public async Task<IActionResult> DeleteDetail(int id, int detailId)
    {
        var ticket = await service.GetByIdAsync(id);
        if (ticket is null || !CanAccess(ticket)) return NotFound();
        var error = await detailService.DeleteAsync(id, detailId);
        if (error == RepairTicketDetailError.None && !await ReturnToApprovalIfNeeded(ticket, "Hạng mục bị xóa khi đang sửa; chờ khách duyệt lại báo giá."))
            return Conflict(new ProblemDetails { Title = "Không thể chuyển phiếu về chờ duyệt." });
        return error == RepairTicketDetailError.None ? NoContent() : DetailError(error);
    }

    [HttpPost("{id:int}/details/services")]
    public async Task<ActionResult<RepairTicketDetailResponse>> AddRepairService(
        int id, AddRepairServiceDetailRequest request)
    {
        var ticket = await service.GetByIdAsync(id);
        if (ticket is null || !CanAccess(ticket)) return NotFound();
        var result = await detailService.AddRepairServiceAsync(
            id, request.RepairServiceId, request.Quantity, request.DiscountPercent,
            request.WarrantyDuration, request.WarrantyDurationUnit, request.WarrantyTerms);
        if (result.Error != RepairTicketDetailError.None)
            return DetailError(result.Error);

        if (!await ReturnToApprovalIfNeeded(ticket, "Phát sinh dịch vụ khi đang sửa; chờ khách duyệt lại báo giá."))
            return Conflict(new ProblemDetails { Title = "Không thể chuyển phiếu về chờ duyệt." });

        return Ok(ToDetailResponse(result.Value!));
    }

    [HttpPost("{id:int}/details/spare-parts")]
    public async Task<ActionResult<RepairTicketDetailResponse>> AddSparePart(
        int id, AddSparePartDetailRequest request)
    {
        var ticket = await service.GetByIdAsync(id);
        if (ticket is null || !CanAccess(ticket)) return NotFound();
        var result = await detailService.AddSparePartAsync(
            id, request.SparePartId, request.Quantity, request.DiscountPercent,
            request.WarrantyDuration, request.WarrantyDurationUnit, request.WarrantyTerms);
        if (result.Error != RepairTicketDetailError.None)
            return DetailError(result.Error);

        if (!await ReturnToApprovalIfNeeded(ticket, "Phát sinh phụ tùng khi đang sửa; chờ khách duyệt lại báo giá."))
            return Conflict(new ProblemDetails { Title = "Không thể chuyển phiếu về chờ duyệt." });

        return Ok(ToDetailResponse(result.Value!));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Receptionist")]
    public async Task<ActionResult<RepairTicketResponse>> Create(CreateRepairTicketRequest request)
    {
        var ticket = new RepairTicket
        {
            TicketNumber = string.Empty,
            CustomerId = request.CustomerId,
            DeviceId = request.DeviceId,
            IssueDescription = request.IssueDescription,
            CustomerNotes = request.CustomerNotes
        };

        var result = await service.CreateAsync(ticket);
        if (result.Error == RepairTicketOperationError.CustomerNotFound)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Không tìm thấy khách hàng",
                Detail = $"CustomerId {request.CustomerId} không tồn tại."
            });
        }

        if (result.Error == RepairTicketOperationError.DeviceNotFoundForCustomer)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Không tìm thấy thiết bị của khách hàng",
                Detail = $"DeviceId {request.DeviceId} không tồn tại hoặc không thuộc CustomerId {request.CustomerId}."
            });
        }

        var created = result.Value!;
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, ToResponse(created));
    }

    private ActionResult<RepairTicketResponse> ToError(RepairTicketOperationError error) => error switch
    {
        RepairTicketOperationError.RepairTicketNotFound => NotFound(),
        RepairTicketOperationError.TechnicianNotFoundOrInactive => BadRequest(new ProblemDetails
        {
            Title = "Kỹ thuật viên không hợp lệ",
            Detail = "TechnicianId không tồn tại hoặc kỹ thuật viên đã ngừng hoạt động."
        }),
        RepairTicketOperationError.TechnicianNotQualifiedForDevice => Conflict(new ProblemDetails
        {
            Title = "Kỹ thuật viên chưa có chuyên môn phù hợp",
            Detail = "Kỹ thuật viên chưa được gán loại thiết bị của phiếu sửa chữa."
        }),
        RepairTicketOperationError.TicketIsClosed => Conflict(new ProblemDetails
        {
            Title = "Phiếu đã đóng",
            Detail = "Không thể phân công lại phiếu đã giao hoặc đã hủy."
        }),
        RepairTicketOperationError.CannotUnassignTechnicianDuringWork => Conflict(new ProblemDetails
        {
            Title = "Không thể bỏ phân công",
            Detail = "Hãy phân công kỹ thuật viên thay thế trước khi tiếp tục công việc."
        }),
        RepairTicketOperationError.InvalidStatusTransition => Conflict(new ProblemDetails
        {
            Title = "Không thể chuyển trạng thái",
            Detail = "Chuyển trạng thái này không hợp lệ theo quy trình sửa chữa."
        }),
        RepairTicketOperationError.ChangedByUserNotFoundOrInactive => BadRequest(new ProblemDetails
        {
            Title = "Người cập nhật không hợp lệ",
            Detail = "ChangedByUserId không tồn tại hoặc tài khoản đã ngừng hoạt động."
        }),
        RepairTicketOperationError.TechnicianRequiredForStatus => Conflict(new ProblemDetails
        {
            Title = "Phiếu chưa được phân công",
            Detail = "Cần phân công kỹ thuật viên trước khi chuyển phiếu sang trạng thái này."
        }),
        _ => Problem(statusCode: StatusCodes.Status500InternalServerError)
    };

    private ActionResult DetailError(RepairTicketDetailError error) => error switch
    {
        RepairTicketDetailError.RepairTicketNotFound => NotFound(new ProblemDetails { Title = "Không tìm thấy phiếu sửa chữa." }),
        RepairTicketDetailError.RepairServiceNotFoundOrInactive => NotFound(new ProblemDetails { Title = "Không tìm thấy dịch vụ hoặc dịch vụ đã ngừng hoạt động." }),
        RepairTicketDetailError.SparePartNotFound => NotFound(new ProblemDetails { Title = "Không tìm thấy phụ tùng." }),
        RepairTicketDetailError.SparePartInactive => Conflict(new ProblemDetails { Title = "Phụ tùng đã ngừng hoạt động, không thể tăng số lượng trên phiếu." }),
        RepairTicketDetailError.RepairTicketClosed => Conflict(new ProblemDetails { Title = "Phiếu đã đóng, không thể thêm dịch vụ hoặc phụ tùng." }),
        RepairTicketDetailError.InsufficientStock => Conflict(new ProblemDetails { Title = "Tồn kho không đủ để xuất phụ tùng." }),
        RepairTicketDetailError.DiscountExceedsSubtotal => BadRequest(new ProblemDetails { Title = "Số lượng hoặc chiết khấu không hợp lệ." }),
        RepairTicketDetailError.RepairTicketNotEditable => Conflict(new ProblemDetails { Title = "Chỉ được sửa hoặc xóa hạng mục khi phiếu đang kiểm tra hoặc chờ khách duyệt." }),
        RepairTicketDetailError.DetailNotFound => NotFound(new ProblemDetails { Title = "Không tìm thấy hạng mục trên phiếu." }),
        RepairTicketDetailError.InvalidAmounts => BadRequest(new ProblemDetails { Title = "Số lượng hoặc chiết khấu không hợp lệ." }),
        RepairTicketDetailError.InvalidWarrantyPolicy => BadRequest(new ProblemDetails { Title = "Thời hạn bảo hành tối đa là 120 tháng hoặc 36.500 ngày." }),
        _ => Problem(statusCode: StatusCodes.Status500InternalServerError)
    };

    private static RepairTicketDetailResponse ToDetailResponse(RepairTicketDetail detail) => new()
    {
        Id = detail.Id,
        RepairTicketId = detail.RepairTicketId,
        ItemType = detail.RepairServiceId.HasValue ? "Service" : "SparePart",
        RepairServiceId = detail.RepairServiceId,
        SparePartId = detail.SparePartId,
        Description = detail.Description,
        Quantity = detail.Quantity,
        UnitPrice = detail.UnitPrice,
        DiscountPercent = detail.DiscountPercent,
        DiscountAmount = detail.DiscountAmount,
        TotalAmount = detail.TotalAmount,
        WarrantyDuration = detail.WarrantyDuration,
        WarrantyDurationUnit = detail.WarrantyDurationUnit,
        WarrantyTerms = detail.WarrantyTerms,
        WarrantyStartDate = detail.WarrantyStartDate,
        WarrantyEndDate = detail.WarrantyEndDate
    };

    private static RepairTicketResponse ToResponse(RepairTicket ticket) => new()
    {
        Id = ticket.Id,
        TicketNumber = ticket.TicketNumber,
        CustomerId = ticket.CustomerId,
        CustomerName = ticket.Customer?.Name ?? string.Empty,
        CustomerPhoneNumber = ticket.Customer?.PhoneNumber,
        DeviceId = ticket.DeviceId,
        DeviceTypeId = ticket.Device.DeviceTypeId,
        TechnicianId = ticket.TechnicianId,
        ParentRepairTicketId = ticket.ParentRepairTicketId,
        ParentRepairTicketNumber = ticket.ParentRepairTicket?.TicketNumber,
        IsWarrantyRepair = ticket.IsWarrantyRepair,
        TechnicianName = ticket.Technician?.FullName,
        DeviceType = ticket.Device?.DeviceType?.Name ?? string.Empty,
        DeviceBrand = ticket.Device?.Brand ?? string.Empty,
        DeviceModel = ticket.Device?.Model ?? string.Empty,
        SerialNumber = ticket.Device?.SerialNumber,
        Status = ticket.Status,
        IssueDescription = ticket.IssueDescription,
        CustomerNotes = ticket.CustomerNotes,
        ReceivedAt = ticket.ReceivedAt
    };

    private bool CanAccess(RepairTicket ticket) => User.IsInRole("Admin") || !User.IsInRole("Technician") ||
        (int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) && ticket.Technician?.UserId == userId);

    private bool CanChangeStatus(RepairStatus current, RepairStatus next)
    {
        if (User.IsInRole("Admin")) return true;

        if (User.IsInRole("Receptionist"))
        {
            return (current, next) is
                (RepairStatus.Received, RepairStatus.Diagnosing) or
                (RepairStatus.Diagnosing, RepairStatus.WaitingForApproval) or
                (RepairStatus.WaitingForApproval, RepairStatus.InProgress) or
                (RepairStatus.WaitingForApproval, RepairStatus.Cancelled) or
                (RepairStatus.Completed, RepairStatus.Delivered);
        }

        if (User.IsInRole("Technician"))
        {
            return (current, next) is
                (RepairStatus.Diagnosing, RepairStatus.WaitingForApproval) or
                (RepairStatus.InProgress, RepairStatus.WaitingForParts) or
                (RepairStatus.WaitingForParts, RepairStatus.InProgress) or
                (RepairStatus.InProgress, RepairStatus.Completed);
        }

        return false;
    }

    private async Task<bool> ReturnToApprovalIfNeeded(RepairTicket ticket, string note)
    {
        if (ticket.Status != RepairStatus.InProgress) return true;
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return false;
        var result = await service.ChangeStatusAsync(ticket.Id, RepairStatus.WaitingForApproval, userId, note);
        return result.Error == RepairTicketOperationError.None;
    }
}
