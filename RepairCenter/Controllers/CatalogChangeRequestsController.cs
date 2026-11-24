using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RepairCenter.DTOs.CatalogChangeRequests;
using RepairCenter.Entities;
using RepairCenter.Services.Interfaces;

namespace RepairCenter.Controllers;

[ApiController]
[Route("api/catalog-change-requests")]
[Authorize(Roles = "Admin,Receptionist,Technician")]
public sealed class CatalogChangeRequestsController(ICatalogChangeRequestService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        int? userId = null;
        if (!User.IsInRole("Admin"))
        {
            if (!TryGetUserId(out var id)) return Unauthorized();
            userId = id;
        }
        var rows = await service.GetAllAsync(User.IsInRole("Admin"), userId);
        return Ok(rows.Select(ToResponse));
    }

    [HttpPost]
    [Authorize(Roles = "Technician")]
    public async Task<IActionResult> Create(CreateCatalogChangeRequest request)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await service.CreateAsync(request, userId);
        if (result.Error != CatalogChangeRequestError.None) return ToError(result);
        return Created("/api/catalog-change-requests", ToResponse(result.Value!));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Cancel(int id)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await service.CancelAsync(id, userId);
        if (result.Error != CatalogChangeRequestError.None) return ToError(result);
        return NoContent();
    }

    [HttpPost("{id:int}/approve")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Approve(int id, ResolveCatalogChangeRequest request)
    {
        if (!TryGetUserId(out var adminId)) return Unauthorized();
        var result = await service.ApproveAsync(id, adminId, request);
        if (result.Error != CatalogChangeRequestError.None) return ToError(result);
        return Ok(ToResponse(result.Value!));
    }

    [HttpPost("{id:int}/reject")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Reject(int id, ResolveCatalogChangeRequest request)
    {
        if (!TryGetUserId(out var adminId)) return Unauthorized();
        var result = await service.RejectAsync(id, adminId, request);
        if (result.Error != CatalogChangeRequestError.None) return ToError(result);
        return Ok(ToResponse(result.Value!));
    }

    private IActionResult ToError<T>(CatalogChangeRequestResult<T> result)
    {
        var problem = new ProblemDetails { Title = result.Title, Detail = result.Detail };
        return result.Error switch
        {
            CatalogChangeRequestError.Invalid => BadRequest(problem),
            CatalogChangeRequestError.NotFound => NotFound(problem),
            CatalogChangeRequestError.Conflict => Conflict(problem),
            _ => Problem(statusCode: StatusCodes.Status500InternalServerError)
        };
    }

    private bool TryGetUserId(out int id) => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out id);

    private static object ToResponse(CatalogChangeRequest row) => new
    {
        row.Id,
        row.ItemType,
        row.Action,
        row.TargetId,
        Payload = JsonSerializer.Deserialize<JsonElement>(row.PayloadJson),
        row.Status,
        row.RequestedByUserId,
        RequestedBy = row.RequestedByUser?.FullName,
        row.RequestedAt,
        row.ResolvedByUserId,
        row.ResolvedAt,
        row.ResolutionNote
    };
}
