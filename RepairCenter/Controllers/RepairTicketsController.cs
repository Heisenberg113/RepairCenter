using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using RepairCenter.DTOs.RepairTickets;
using RepairCenter.Entities;
using RepairCenter.Enums;
using RepairCenter.Services.Interfaces;
using RepairCenter.Contracts;

namespace RepairCenter.Controllers;

[ApiController]
[Route("api/repair-tickets")]
[Authorize(Roles = "Admin,Receptionist,Technician")]
public class RepairTicketsController(
    IRepairTicketService service,
    IRepairTicketDetailService detailService,
    IRepairIntakeService intakeService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedRepairTicketResponse>> GetAll(
        [FromQuery] int? customerId,
        [FromQuery] int? deviceId,
        [FromQuery] int? technicianId,
        [FromQuery] RepairStatus? status,
        [FromQuery] DateTime? receivedFrom,
        [FromQuery] DateTime? receivedTo,
        [FromQuery] string? search,
        [FromQuery] bool oldestFirst = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        if (page < 1 || pageSize is < 1 or > 100 ||
            page > int.MaxValue / pageSize + 1 || receivedFrom > receivedTo)
            return BadRequest(new ProblemDetails { Title = "Tham số phân trang hoặc bộ lọc không hợp lệ." });

        int? technicianUserId = null;
        if (User.IsInRole("Technician") && !User.IsInRole("Admin"))
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return Unauthorized();
            technicianUserId = userId;
        }
        var result = await service.GetAllAsync(page, pageSize, customerId, deviceId, technicianId,
            technicianUserId, status, receivedFrom, receivedTo, search, oldestFirst);
        return Ok(new PagedRepairTicketResponse
        {
            Items = result.Items.Select(ToResponse),
            Page = result.Page,
            PageSize = result.PageSize,
            TotalCount = result.TotalCount,
            ActiveCount = result.ActiveCount
        });
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

    [HttpGet("devices/{deviceId:int}/open")]
    [Authorize(Roles = "Admin,Receptionist")]
    public async Task<ActionResult<IEnumerable<RepairTicketResponse>>> GetOpenByDevice(int deviceId)
    {
        var tickets = await service.GetOpenByDeviceAsync(deviceId);
        return Ok(tickets.Select(ToResponse));
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
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var changedByUserId))
            return Unauthorized();
        var ticket = await service.GetByIdAsync(id);
        if (ticket is null || !CanAccess(ticket)) return NotFound();
        var error = await detailService.UpdateAsync(id, detailId, request.Quantity, request.DiscountPercent, changedByUserId);
        return error == RepairTicketDetailError.None ? NoContent() : DetailError(error);
    }

    [HttpDelete("{id:int}/details/{detailId:int}")]
    public async Task<IActionResult> DeleteDetail(int id, int detailId)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var changedByUserId))
            return Unauthorized();
        var ticket = await service.GetByIdAsync(id);
        if (ticket is null || !CanAccess(ticket)) return NotFound();
        var error = await detailService.DeleteAsync(id, detailId, changedByUserId);
        return error == RepairTicketDetailError.None ? NoContent() : DetailError(error);
    }

    [HttpPost("{id:int}/details/services")]
    public async Task<ActionResult<RepairTicketDetailResponse>> AddRepairService(
        int id, AddRepairServiceDetailRequest request)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var changedByUserId))
            return Unauthorized();
        var ticket = await service.GetByIdAsync(id);
        if (ticket is null || !CanAccess(ticket)) return NotFound();
        var result = await detailService.AddRepairServiceAsync(
            id, request.RepairServiceId, request.Quantity, request.DiscountPercent, changedByUserId,
            request.WarrantyDuration, request.WarrantyDurationUnit, request.WarrantyTerms);
        if (result.Error != RepairTicketDetailError.None)
            return DetailError(result.Error);

        return Ok(ToDetailResponse(result.Value!));
    }

    [HttpPost("{id:int}/details/spare-parts")]
    public async Task<ActionResult<RepairTicketDetailResponse>> AddSparePart(
        int id, AddSparePartDetailRequest request)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var changedByUserId))
            return Unauthorized();
        var ticket = await service.GetByIdAsync(id);
        if (ticket is null || !CanAccess(ticket)) return NotFound();
        var result = await detailService.AddSparePartAsync(
            id, request.SparePartId, request.Quantity, request.DiscountPercent, changedByUserId,
            request.WarrantyDuration, request.WarrantyDurationUnit, request.WarrantyTerms);
        if (result.Error != RepairTicketDetailError.None)
            return DetailError(result.Error);

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

    [HttpPost("intake")]
    [Authorize(Roles = "Admin,Receptionist")]
    public async Task<ActionResult<RepairTicketResponse>> CreateIntake(CreateRepairIntakeRequest request)
    {
        var (ticket, error) = await intakeService.CreateAsync(request);
        if (error == RepairIntakeError.CustomerNotFound)
            return NotFound(new ProblemDetails { Title = "Không tìm thấy khách hàng." });
        if (error == RepairIntakeError.DeviceNotFoundForCustomer)
            return NotFound(new ProblemDetails { Title = "Thiết bị không tồn tại hoặc không thuộc khách hàng đã chọn." });
        if (error == RepairIntakeError.DeviceTypeNotFound)
            return NotFound(new ProblemDetails { Title = "Không tìm thấy loại thiết bị." });
        if (ticket is null)
            return Problem(statusCode: StatusCodes.Status500InternalServerError);

        return CreatedAtAction(nameof(GetById), new { id = ticket.Id }, ToResponse(ticket));
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
        RepairTicketOperationError.QuoteNotAccepted => Conflict(new ProblemDetails
        {
            Title = "Báo giá chưa được khách đồng ý",
            Detail = "Cần tạo phiên bản báo giá hiện tại và ghi nhận khách đồng ý trước khi bắt đầu sửa."
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
        RepairTicketDetailError.DeviceTypeMismatch => Conflict(new ProblemDetails { Title = "Loại dịch vụ hoặc phụ tùng không khớp với loại thiết bị của phiếu." }),
        RepairTicketDetailError.SparePartNotFound => NotFound(new ProblemDetails { Title = "Không tìm thấy phụ tùng." }),
        RepairTicketDetailError.SparePartInactive => Conflict(new ProblemDetails { Title = "Phụ tùng đã ngừng hoạt động, không thể tăng số lượng trên phiếu." }),
        RepairTicketDetailError.RepairTicketClosed => Conflict(new ProblemDetails { Title = "Phiếu đã đóng, không thể thêm dịch vụ hoặc phụ tùng." }),
        RepairTicketDetailError.InsufficientStock => Conflict(new ProblemDetails { Title = "Tồn kho không đủ để xuất phụ tùng." }),
        RepairTicketDetailError.DiscountExceedsSubtotal => BadRequest(new ProblemDetails { Title = "Số lượng hoặc chiết khấu không hợp lệ." }),
        RepairTicketDetailError.RepairTicketNotEditable => Conflict(new ProblemDetails { Title = "Chỉ được sửa hoặc xóa hạng mục khi phiếu đang kiểm tra, chờ khách duyệt hoặc đang sửa chữa." }),
        RepairTicketDetailError.DetailNotFound => NotFound(new ProblemDetails { Title = "Không tìm thấy hạng mục trên phiếu." }),
        RepairTicketDetailError.InvalidAmounts => BadRequest(new ProblemDetails { Title = "Số lượng hoặc chiết khấu không hợp lệ." }),
        RepairTicketDetailError.InvalidWarrantyPolicy => BadRequest(new ProblemDetails { Title = "Thời hạn bảo hành tối đa là 120 tháng hoặc 36.500 ngày." }),
        RepairTicketDetailError.ChangedByUserNotFoundOrInactive => Unauthorized(),
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
        WarrantyStartDate = detail.WarrantyStartDate.HasValue
            ? DateOnly.FromDateTime(detail.WarrantyStartDate.Value)
            : null,
        WarrantyEndDate = detail.WarrantyEndDate.HasValue
            ? DateOnly.FromDateTime(detail.WarrantyEndDate.Value)
            : null
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
                (RepairStatus.Received, RepairStatus.Diagnosing) or
                (RepairStatus.Diagnosing, RepairStatus.WaitingForApproval) or
                (RepairStatus.InProgress, RepairStatus.WaitingForParts) or
                (RepairStatus.WaitingForParts, RepairStatus.InProgress) or
                (RepairStatus.InProgress, RepairStatus.Completed);
        }

        return false;
    }

}
