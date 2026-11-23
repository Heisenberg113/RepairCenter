using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RepairCenter.DTOs.DeviceTypes;
using RepairCenter.Entities;
using RepairCenter.Services.Interfaces;

namespace RepairCenter.Controllers;

[ApiController]
[Route("api/device-types")]
[Authorize(Roles = "Admin,Receptionist,Technician")]
public sealed class DeviceTypesController(IDeviceTypeService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<DeviceTypeResponse>>> GetAll() =>
        Ok((await service.GetAllAsync()).Select(ToResponse));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<DeviceTypeResponse>> GetById(int id)
    {
        var deviceType = await service.GetByIdAsync(id);
        return deviceType is null ? NotFound() : Ok(ToResponse(deviceType));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<DeviceTypeResponse>> Create(CreateDeviceTypeRequest request)
    {
        var result = await service.CreateAsync(request.Name);
        if (result.Error != DeviceTypeOperationError.None) return ToError(result.Error);
        var created = result.Value!;
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, ToResponse(created));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<DeviceTypeResponse>> Update(int id, UpdateDeviceTypeRequest request)
    {
        var result = await service.UpdateAsync(id, request.Name);
        return result.Error == DeviceTypeOperationError.None
            ? Ok(ToResponse(result.Value!)) : ToError(result.Error);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var error = await service.DeleteAsync(id);
        return error switch
        {
            DeviceTypeOperationError.None => NoContent(),
            DeviceTypeOperationError.NotFound => NotFound(),
            DeviceTypeOperationError.InUse => Conflict(new ProblemDetails
            {
                Title = "Không thể xóa loại thiết bị",
                Detail = "Loại thiết bị đang được thiết bị, kỹ thuật viên, dịch vụ sửa chữa hoặc phụ tùng sử dụng."
            }),
            _ => Problem(statusCode: StatusCodes.Status500InternalServerError)
        };
    }

    private ActionResult<DeviceTypeResponse> ToError(DeviceTypeOperationError error) => error switch
    {
        DeviceTypeOperationError.NotFound => NotFound(),
        DeviceTypeOperationError.InvalidName => BadRequest(new ProblemDetails
        {
            Title = "Tên loại thiết bị không hợp lệ",
            Detail = "Tên loại thiết bị không được để trống."
        }),
        DeviceTypeOperationError.DuplicateName => Conflict(new ProblemDetails
        {
            Title = "Tên loại thiết bị không hợp lệ",
            Detail = "Tên không được để trống và phải là duy nhất."
        }),
        DeviceTypeOperationError.InUse => Conflict(new ProblemDetails { Title = "Loại thiết bị đang được sử dụng." }),
        _ => Problem(statusCode: StatusCodes.Status500InternalServerError)
    };

    private static DeviceTypeResponse ToResponse(DeviceType deviceType) => new()
    {
        Id = deviceType.Id,
        Name = deviceType.Name
    };
}
