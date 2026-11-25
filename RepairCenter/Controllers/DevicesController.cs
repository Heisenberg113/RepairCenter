using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using RepairCenter.DTOs.Device;
using RepairCenter.Entities;
using RepairCenter.Services.Interfaces;

namespace RepairCenter.Controllers;

[ApiController]
[Route("api/devices")]
[Authorize(Roles = "Admin,Receptionist")]
public class DevicesController(IDeviceService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<DeviceResponse>>> GetAll(
        [FromQuery] int? customerId,
        [FromQuery] int? deviceTypeId)
    {
        var devices = await service.GetAllAsync(customerId, deviceTypeId);
        return Ok(devices.Select(ToResponse));
    }

    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<DeviceResponse>>> Search(
        [FromQuery] int customerId,
        [FromQuery] string? query,
        [FromQuery] int limit = 20)
    {
        if (customerId < 1 || string.IsNullOrWhiteSpace(query) || query.Trim().Length < 2 ||
            limit is < 1 or > 50)
            return BadRequest(new ProblemDetails { Title = "Chọn khách hàng, nhập ít nhất 2 ký tự và giới hạn kết quả từ 1 đến 50." });

        var devices = await service.SearchAsync(customerId, query.Trim(), limit);
        return Ok(devices.Select(ToResponse));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<DeviceResponse>> GetById(int id)
    {
        var device = await service.GetByIdAsync(id);
        return device is null ? NotFound() : Ok(ToResponse(device));
    }

    [HttpPost]
    public async Task<ActionResult<DeviceResponse>> Create(CreateDeviceRequest request)
    {
        var device = new Device
        {
            CustomerId = request.CustomerId,
            DeviceTypeId = request.DeviceTypeId,
            Brand = request.Brand,
            Model = request.Model,
            SerialNumber = request.SerialNumber,
            Color = request.Color,
            Description = request.Description
        };

        var result = await service.CreateAsync(device);
        if (result.Error != DeviceOperationError.None)
            return ToError(result.Error);

        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, ToResponse(result.Value));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<DeviceResponse>> Update(int id, UpdateDeviceRequest request)
    {
        var changes = new Device
        {
            CustomerId = 0,
            DeviceTypeId = request.DeviceTypeId,
            Brand = request.Brand,
            Model = request.Model,
            SerialNumber = request.SerialNumber,
            Color = request.Color,
            Description = request.Description
        };

        var result = await service.UpdateAsync(id, changes);
        if (result.Error != DeviceOperationError.None)
            return ToError(result.Error);

        return Ok(ToResponse(result.Value!));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var result = await service.DeleteAsync(id);
            if (result.Error == DeviceOperationError.DeviceNotFound)
                return NotFound();
            if (result.Error == DeviceOperationError.HasRepairTickets)
                return Conflict(new ProblemDetails
                {
                    Title = "Không thể xóa thiết bị",
                    Detail = "Thiết bị đã có phiếu sửa chữa. Hãy giữ thiết bị để bảo toàn lịch sử."
                });

            return NoContent();
        }
        catch (DbUpdateException)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Không thể xóa thiết bị",
                Detail = "Thiết bị đang được dữ liệu khác tham chiếu."
            });
        }
    }

    private ActionResult<DeviceResponse> ToError(DeviceOperationError error) => error switch
    {
        DeviceOperationError.DeviceNotFound => NotFound(),
        DeviceOperationError.CustomerNotFound => BadRequest(new ProblemDetails
        {
            Title = "Khách hàng không tồn tại",
            Detail = "CustomerId phải tham chiếu đến khách hàng hiện có."
        }),
        DeviceOperationError.DeviceTypeNotFound => BadRequest(new ProblemDetails
        {
            Title = "Loại thiết bị không tồn tại",
            Detail = "DeviceTypeId phải tham chiếu đến loại thiết bị hiện có."
        }),
        _ => Problem(statusCode: StatusCodes.Status500InternalServerError)
    };

    private static DeviceResponse ToResponse(Device device) => new()
    {
        Id = device.Id,
        CustomerId = device.CustomerId,
        CustomerName = device.Customer?.Name,
        DeviceTypeId = device.DeviceTypeId,
        DeviceTypeName = device.DeviceType?.Name,
        Brand = device.Brand,
        Model = device.Model,
        SerialNumber = device.SerialNumber,
        Color = device.Color,
        Description = device.Description
    };
}
