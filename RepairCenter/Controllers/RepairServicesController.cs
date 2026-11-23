using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RepairCenter.DTOs.RepairServices;
using RepairCenter.Entities;
using RepairCenter.Services.Interfaces;

namespace RepairCenter.Controllers;

[ApiController]
[Route("api/repair-services")]
[Authorize(Roles = "Admin,Receptionist,Technician")]
public sealed class RepairServicesController(IRepairServiceService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<RepairServiceResponse>>> GetAll([FromQuery] bool includeInactive = false) =>
        Ok((await service.GetAllAsync(User.IsInRole("Admin") && includeInactive)).Select(ToResponse));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<RepairServiceResponse>> GetById(int id)
    {
        var item = await service.GetByIdAsync(id);
        return item is null ? NotFound() : Ok(ToResponse(item));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<RepairServiceResponse>> Create(CreateRepairServiceRequest request)
    {
        var result = await service.CreateAsync(request.DeviceTypeId, request.Name, request.Description, request.UnitPrice, request.WarrantyDuration, request.WarrantyDurationUnit, request.WarrantyTerms);
        if (result.Error != RepairServiceOperationError.None) return ToError(result.Error);
        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, ToResponse(result.Value));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<RepairServiceResponse>> Update(int id, UpdateRepairServiceRequest request)
    {
        var result = await service.UpdateAsync(id, request.DeviceTypeId, request.Name, request.Description, request.UnitPrice, request.IsActive, request.WarrantyDuration, request.WarrantyDurationUnit, request.WarrantyTerms);
        return result.Error == RepairServiceOperationError.None ? Ok(ToResponse(result.Value!)) : ToError(result.Error);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id) => (await service.DeleteAsync(id)) switch
    {
        RepairServiceOperationError.None => NoContent(),
        RepairServiceOperationError.NotFound => NotFound(),
        RepairServiceOperationError.InUse => Conflict(new ProblemDetails
        {
            Title = "Dịch vụ đã được sử dụng",
            Detail = "Không thể xóa dịch vụ đã nằm trên phiếu sửa chữa. Hãy chuyển dịch vụ sang ngừng hoạt động."
        }),
        _ => Problem(statusCode: StatusCodes.Status500InternalServerError)
    };

    private ActionResult<RepairServiceResponse> ToError(RepairServiceOperationError error) => error switch
    {
        RepairServiceOperationError.NotFound => NotFound(),
        RepairServiceOperationError.InvalidName => BadRequest(new ProblemDetails { Title = "Tên dịch vụ không hợp lệ." }),
        RepairServiceOperationError.DuplicateName => Conflict(new ProblemDetails { Title = "Tên dịch vụ đã tồn tại." }),
        RepairServiceOperationError.InvalidPrice => BadRequest(new ProblemDetails { Title = "Đơn giá không được âm." }),
        RepairServiceOperationError.InvalidWarranty => BadRequest(new ProblemDetails { Title = "Thời hạn hoặc đơn vị bảo hành không hợp lệ." }),
        RepairServiceOperationError.InvalidDeviceType => BadRequest(new ProblemDetails { Title = "Vui lòng chọn loại thiết bị hợp lệ." }),
        _ => Problem(statusCode: StatusCodes.Status500InternalServerError)
    };

    private static RepairServiceResponse ToResponse(RepairService item) => new()
    {
        Id = item.Id, DeviceTypeId = item.DeviceTypeId, DeviceTypeName = item.DeviceType?.Name,
        Name = item.Name, Description = item.Description, UnitPrice = item.UnitPrice, IsActive = item.IsActive,
        WarrantyDuration = item.WarrantyDuration, WarrantyDurationUnit = item.WarrantyDurationUnit, WarrantyTerms = item.WarrantyTerms
    };
}
