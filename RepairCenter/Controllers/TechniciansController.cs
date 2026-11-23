using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using RepairCenter.DTOs.Technicians;
using RepairCenter.Entities;
using RepairCenter.Services.Interfaces;

namespace RepairCenter.Controllers;

[ApiController]
[Route("api/technicians")]
[Authorize(Roles = "Admin,Receptionist")]
public class TechniciansController(ITechnicianService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TechnicianResponse>>> GetAll()
    {
        var technicians = await service.GetAllAsync();
        return Ok(technicians.Select(ToResponse));
    }

    [HttpGet("device-types")]
    public async Task<ActionResult<IEnumerable<DeviceTypeOptionResponse>>> GetDeviceTypes()
    {
        var deviceTypes = await service.GetDeviceTypesAsync();
        return Ok(deviceTypes.Select(type => new DeviceTypeOptionResponse
        {
            Id = type.Id,
            Name = type.Name
        }));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TechnicianResponse>> GetById(int id)
    {
        var technician = await service.GetByIdAsync(id);
        return technician is null ? NotFound() : Ok(ToResponse(technician));
    }

    [HttpPost]
    public async Task<ActionResult<TechnicianResponse>> Create(CreateTechnicianRequest request)
    {
        var technician = new Technician
        {
            UserId = request.UserId,
            EmployeeCode = request.EmployeeCode,
            FullName = request.FullName,
            PhoneNumber = request.PhoneNumber,
            Email = request.Email,
            Specialty = request.Specialty,
            IsActive = request.IsActive
        };

        var result = await service.CreateAsync(technician, request.DeviceTypeIds);
        if (result.Error != TechnicianOperationError.None)
            return ToError(result.Error);

        var created = result.Value!;
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, ToResponse(created));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<TechnicianResponse>> Update(int id, UpdateTechnicianRequest request)
    {
        var changes = new Technician
        {
            UserId = request.UserId,
            EmployeeCode = request.EmployeeCode,
            FullName = request.FullName,
            PhoneNumber = request.PhoneNumber,
            Email = request.Email,
            Specialty = request.Specialty,
            IsActive = request.IsActive
        };

        var result = await service.UpdateAsync(id, changes, request.DeviceTypeIds);
        if (result.Error != TechnicianOperationError.None)
            return ToError(result.Error);

        return Ok(ToResponse(result.Value!));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var result = await service.DeleteAsync(id);
            if (result.Error == TechnicianOperationError.TechnicianNotFound)
                return NotFound();
            if (result.Error == TechnicianOperationError.HasRepairTickets)
                return Conflict(new ProblemDetails
                {
                    Title = "Không thể xóa kỹ thuật viên",
                    Detail = "Kỹ thuật viên đã được gán cho phiếu sửa chữa. Hãy đánh dấu IsActive=false để giữ lịch sử."
                });
            if (result.Error == TechnicianOperationError.AccountRequiresTechnicianProfile)
                return Conflict(new ProblemDetails
                {
                    Title = "Không thể xóa hồ sơ kỹ thuật viên đang gắn với tài khoản Technician",
                    Detail = "Hãy gỡ role Technician hoặc liên kết tài khoản đó với hồ sơ khác trước khi xóa."
                });

            return NoContent();
        }
        catch (DbUpdateException)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Không thể xóa kỹ thuật viên",
                Detail = "Kỹ thuật viên đang được dữ liệu khác tham chiếu."
            });
        }
    }

    private ActionResult<TechnicianResponse> ToError(TechnicianOperationError error) => error switch
    {
        TechnicianOperationError.TechnicianNotFound => NotFound(),
        TechnicianOperationError.UserNotFoundOrAlreadyLinked => BadRequest(new ProblemDetails
        {
            Title = "User không hợp lệ",
            Detail = "UserId không tồn tại hoặc đã được liên kết với kỹ thuật viên khác."
        }),
        TechnicianOperationError.DuplicateEmployeeCode => Conflict(new ProblemDetails
        {
            Title = "Mã nhân viên đã tồn tại",
            Detail = "EmployeeCode phải là duy nhất."
        }),
        TechnicianOperationError.DeviceTypesNotFound => BadRequest(new ProblemDetails
        {
            Title = "Loại thiết bị không hợp lệ",
            Detail = "Một hoặc nhiều DeviceTypeId không tồn tại."
        }),
        TechnicianOperationError.HasRepairTickets => Conflict(new ProblemDetails
        {
            Title = "Không thể xóa kỹ thuật viên",
            Detail = "Kỹ thuật viên đã được gán cho phiếu sửa chữa. Hãy đánh dấu IsActive=false để ngừng sử dụng và giữ lịch sử."
        }),
        TechnicianOperationError.AccountRequiresTechnicianProfile => Conflict(new ProblemDetails
        {
            Title = "Tài khoản Technician phải được liên kết với hồ sơ kỹ thuật viên.",
            Detail = "Hãy gỡ role Technician trước khi bỏ liên kết tài khoản khỏi hồ sơ này."
        }),
        _ => Problem(statusCode: StatusCodes.Status500InternalServerError)
    };

    private static TechnicianResponse ToResponse(Technician technician) => new()
    {
        Id = technician.Id,
        UserId = technician.UserId,
        EmployeeCode = technician.EmployeeCode,
        FullName = technician.FullName,
        PhoneNumber = technician.PhoneNumber,
        Email = technician.Email,
        Specialty = technician.Specialty,
        IsActive = technician.IsActive,
        DeviceTypes = technician.DeviceTypes
            .Select(link => new DeviceTypeOptionResponse
            {
                Id = link.DeviceTypeId,
                Name = link.DeviceType.Name
            })
            .OrderBy(type => type.Name)
            .ToList()
    };
}
