using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RepairCenter.Data;
using RepairCenter.DTOs.CatalogChangeRequests;
using RepairCenter.Entities;
using RepairCenter.Enums;

namespace RepairCenter.Controllers;

[ApiController]
[Route("api/catalog-change-requests")]
[Authorize(Roles = "Admin,Receptionist,Technician")]
public sealed class CatalogChangeRequestsController(RepairCenterDbContext db) : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var query = db.CatalogChangeRequests.AsNoTracking().Include(x => x.RequestedByUser).AsQueryable();
        if (User.IsInRole("Admin"))
            query = query.Where(x => x.Status == "Pending");
        else
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();
            query = query.Where(x => x.RequestedByUserId == userId);
        }
        var rows = await query.OrderByDescending(x => x.RequestedAt).Take(200).ToListAsync();
        return Ok(rows.Select(ToResponse));
    }

    [HttpPost]
    [Authorize(Roles = "Technician")]
    public async Task<IActionResult> Create(CreateCatalogChangeRequest request)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var type = request.ItemType.Trim();
        var action = request.Action.Trim();
        if (type is not ("SparePart" or "RepairService") || action is not ("Create" or "Update" or "Deactivate"))
            return BadRequest(new ProblemDetails { Title = "Loại yêu cầu không hợp lệ." });
        if (action == "Create" && request.TargetId.HasValue || action != "Create" && (!request.TargetId.HasValue || request.TargetId <= 0))
            return BadRequest(new ProblemDetails { Title = "Mã danh mục không hợp lệ." });
        if (action != "Create" && !await TargetExists(type, request.TargetId!.Value)) return NotFound(new ProblemDetails { Title = "Không tìm thấy mục danh mục." });
        if (action != "Deactivate")
        {
            var error = await ValidatePayload(request, type, action);
            if (error is not null) return BadRequest(new ProblemDetails { Title = "Thông tin yêu cầu không hợp lệ.", Detail = error });
        }
        var row = new CatalogChangeRequest
        {
            ItemType = type, Action = action, TargetId = request.TargetId,
            PayloadJson = JsonSerializer.Serialize(request, JsonOptions), RequestedByUserId = userId
        };
        db.CatalogChangeRequests.Add(row);
        await db.SaveChangesAsync();
        await db.Entry(row).Reference(x => x.RequestedByUser).LoadAsync();
        return Created("/api/catalog-change-requests", ToResponse(row));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin,Receptionist,Technician")]
    public async Task<IActionResult> Cancel(int id)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var row = await db.CatalogChangeRequests.FirstOrDefaultAsync(x => x.Id == id && x.RequestedByUserId == userId);
        if (row is null) return NotFound();
        if (row.Status != "Pending") return Conflict(new ProblemDetails { Title = "Yêu cầu đã được xử lý, không thể hủy." });
        row.Status = "Cancelled";
        row.ResolvedAt = DateTime.UtcNow;
        row.ResolutionNote = "Đã hủy bởi người gửi.";
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id:int}/approve")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Approve(int id, ResolveCatalogChangeRequest request)
    {
        if (!TryGetUserId(out var adminId)) return Unauthorized();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var row = await db.CatalogChangeRequests.FirstOrDefaultAsync(x => x.Id == id);
        if (row is null) return NotFound();
        if (row.Status != "Pending") return Conflict(new ProblemDetails { Title = "Yêu cầu không còn ở trạng thái chờ duyệt." });
        try
        {
            var applyError = await Apply(row);
            if (applyError is not null) return Conflict(new ProblemDetails { Title = "Không thể áp dụng yêu cầu.", Detail = applyError });
            row.Status = "Approved";
            row.ResolvedByUserId = adminId;
            row.ResolvedAt = DateTime.UtcNow;
            row.ResolutionNote = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
            return Ok(ToResponse(row));
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync();
            return Conflict(new ProblemDetails { Title = "Không thể áp dụng yêu cầu.", Detail = "Mã phụ tùng hoặc tên dịch vụ có thể đã được sử dụng." });
        }
    }

    [HttpPost("{id:int}/reject")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Reject(int id, ResolveCatalogChangeRequest request)
    {
        if (!TryGetUserId(out var adminId)) return Unauthorized();
        var row = await db.CatalogChangeRequests.FirstOrDefaultAsync(x => x.Id == id);
        if (row is null) return NotFound();
        if (row.Status != "Pending") return Conflict(new ProblemDetails { Title = "Yêu cầu không còn ở trạng thái chờ duyệt." });
        if (string.IsNullOrWhiteSpace(request.Note)) return BadRequest(new ProblemDetails { Title = "Cần ghi lý do từ chối." });
        row.Status = "Rejected";
        row.ResolvedByUserId = adminId;
        row.ResolvedAt = DateTime.UtcNow;
        row.ResolutionNote = request.Note.Trim();
        await db.SaveChangesAsync();
        return Ok(ToResponse(row));
    }

    private async Task<string?> ValidatePayload(CreateCatalogChangeRequest r, string type, string action)
    {
        if (string.IsNullOrWhiteSpace(r.Name) || r.Name.Trim().Length > 150) return "Tên là bắt buộc và tối đa 150 ký tự.";
        if ((r.Description?.Length ?? 0) > 1000 || (r.WarrantyTerms?.Length ?? 0) > 1000) return "Mô tả hoặc điều kiện bảo hành vượt quá 1.000 ký tự.";
        if (r.WarrantyDuration is null or < 0 or > 36500 || r.WarrantyDurationUnit is not (1 or 2)) return "Thời hạn bảo hành phải từ 0 đến 36.500 ngày hoặc 120 tháng.";
        if (r.WarrantyDurationUnit == 2 && r.WarrantyDuration > 120) return "Thời hạn bảo hành tối đa là 120 tháng.";
        if (r.DeviceTypeId is null or <= 0 || !await db.DeviceTypes.AnyAsync(x => x.Id == r.DeviceTypeId.Value)) return "Vui lòng chọn loại thiết bị hợp lệ.";
        if (type == "RepairService")
        {
            if (r.UnitPrice is null or < 0) return "Đơn giá dịch vụ không hợp lệ.";
            if (action == "Create" && await db.RepairServices.AnyAsync(x => x.Name == r.Name!.Trim())) return "Tên dịch vụ đã tồn tại.";
            if (action == "Update" && await db.RepairServices.AnyAsync(x => x.Name == r.Name!.Trim() && x.Id != r.TargetId)) return "Tên dịch vụ đã tồn tại.";
        }
        else
        {
            if (string.IsNullOrWhiteSpace(r.PartNumber) || r.PartNumber.Trim().Length > 50) return "Mã phụ tùng là bắt buộc và tối đa 50 ký tự.";
            if (r.UnitCost is null or < 0 || r.SellingPrice is null or < 0 || r.ReorderLevel is null or < 0) return "Giá vốn, giá bán và mức tồn tối thiểu phải không âm.";
            if (action == "Create" && await db.SpareParts.AnyAsync(x => x.PartNumber == r.PartNumber.Trim())) return "Mã phụ tùng đã tồn tại.";
            if (action == "Update" && await db.SpareParts.AnyAsync(x => x.PartNumber == r.PartNumber.Trim() && x.Id != r.TargetId)) return "Mã phụ tùng đã tồn tại.";
        }
        return null;
    }

    private Task<bool> TargetExists(string type, int id) => type == "RepairService"
        ? db.RepairServices.AnyAsync(x => x.Id == id)
        : db.SpareParts.AnyAsync(x => x.Id == id);

    private async Task<string?> Apply(CatalogChangeRequest row)
    {
        var payload = JsonSerializer.Deserialize<CreateCatalogChangeRequest>(row.PayloadJson, JsonOptions);
        if (row.Action == "Create" && payload is not null)
        {
            if (row.ItemType == "RepairService")
                db.RepairServices.Add(new RepairService { DeviceTypeId = payload.DeviceTypeId, Name = payload.Name!.Trim(), Description = payload.Description?.Trim(), UnitPrice = payload.UnitPrice!.Value, WarrantyDuration = payload.WarrantyDuration!.Value, WarrantyDurationUnit = (WarrantyDurationUnit)payload.WarrantyDurationUnit!.Value, WarrantyTerms = payload.WarrantyTerms?.Trim() });
            else
                db.SpareParts.Add(new SparePart { DeviceTypeId = payload.DeviceTypeId, PartNumber = payload.PartNumber!.Trim(), Name = payload.Name!.Trim(), Description = payload.Description?.Trim(), UnitCost = payload.UnitCost!.Value, SellingPrice = payload.SellingPrice!.Value, ReorderLevel = payload.ReorderLevel!.Value, WarrantyDuration = payload.WarrantyDuration!.Value, WarrantyDurationUnit = (WarrantyDurationUnit)payload.WarrantyDurationUnit!.Value, WarrantyTerms = payload.WarrantyTerms?.Trim(), QuantityInStock = 0 });
            return null;
        }
        if (row.TargetId is null) return "Thiếu mã danh mục.";
        if (row.ItemType == "RepairService")
        {
            var item = await db.RepairServices.FirstOrDefaultAsync(x => x.Id == row.TargetId);
            if (item is null) return "Dịch vụ đã bị xóa.";
            if (row.Action == "Deactivate") item.IsActive = false;
            else if (payload is not null)
            {
                if (payload.DeviceTypeId.HasValue) item.DeviceTypeId = payload.DeviceTypeId; item.Name = payload.Name!.Trim(); item.Description = payload.Description?.Trim(); item.UnitPrice = payload.UnitPrice!.Value;
                item.WarrantyDuration = payload.WarrantyDuration!.Value; item.WarrantyDurationUnit = (WarrantyDurationUnit)payload.WarrantyDurationUnit!.Value; item.WarrantyTerms = payload.WarrantyTerms?.Trim();
            }
        }
        else
        {
            var item = await db.SpareParts.FirstOrDefaultAsync(x => x.Id == row.TargetId);
            if (item is null) return "Phụ tùng đã bị xóa.";
            if (row.Action == "Deactivate") item.IsActive = false;
            else if (payload is not null)
            {
                if (payload.DeviceTypeId.HasValue) item.DeviceTypeId = payload.DeviceTypeId; item.PartNumber = payload.PartNumber!.Trim(); item.Name = payload.Name!.Trim(); item.Description = payload.Description?.Trim();
                item.UnitCost = payload.UnitCost!.Value; item.SellingPrice = payload.SellingPrice!.Value; item.ReorderLevel = payload.ReorderLevel!.Value;
                item.WarrantyDuration = payload.WarrantyDuration!.Value; item.WarrantyDurationUnit = (WarrantyDurationUnit)payload.WarrantyDurationUnit!.Value; item.WarrantyTerms = payload.WarrantyTerms?.Trim();
            }
        }
        return null;
    }

    private bool TryGetUserId(out int id) => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out id);

    private static object ToResponse(CatalogChangeRequest x) => new
    {
        x.Id, x.ItemType, x.Action, x.TargetId, Payload = JsonSerializer.Deserialize<JsonElement>(x.PayloadJson), x.Status,
        x.RequestedByUserId, RequestedBy = x.RequestedByUser?.FullName, x.RequestedAt,
        x.ResolvedByUserId, x.ResolvedAt, x.ResolutionNote
    };
}
