using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RepairCenter.DTOs.CatalogChangeRequests;
using RepairCenter.Entities;
using RepairCenter.Enums;
using RepairCenter.Repositories.Interfaces;
using RepairCenter.Services.Interfaces;

namespace RepairCenter.Services;

public sealed class CatalogChangeRequestService(ICatalogChangeRequestRepository repository) : ICatalogChangeRequestService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public Task<List<CatalogChangeRequest>> GetAllAsync(bool isAdmin, int? userId) =>
        repository.GetAllAsync(isAdmin, userId);

    public async Task<CatalogChangeRequestResult<CatalogChangeRequest>> CreateAsync(
        CreateCatalogChangeRequest request, int userId)
    {
        var type = request.ItemType.Trim();
        var action = request.Action.Trim();
        if (type is not ("SparePart" or "RepairService") || action is not ("Create" or "Update" or "Deactivate"))
            return Invalid<CatalogChangeRequest>("Loại yêu cầu không hợp lệ.");
        if (action == "Create" && request.TargetId.HasValue ||
            action != "Create" && (!request.TargetId.HasValue || request.TargetId <= 0))
            return Invalid<CatalogChangeRequest>("Mã danh mục không hợp lệ.");
        if (action != "Create" && !await repository.TargetExistsAsync(type, request.TargetId!.Value))
            return NotFound<CatalogChangeRequest>("Không tìm thấy mục danh mục.");
        if (action != "Deactivate")
        {
            var validationError = await ValidatePayloadAsync(request, type, action);
            if (validationError is not null)
                return Invalid<CatalogChangeRequest>("Thông tin yêu cầu không hợp lệ.", validationError);
        }

        var row = new CatalogChangeRequest
        {
            ItemType = type,
            Action = action,
            TargetId = request.TargetId,
            PayloadJson = JsonSerializer.Serialize(request, JsonOptions),
            RequestedByUserId = userId
        };
        await repository.AddAsync(row);
        return Success(row);
    }

    public async Task<CatalogChangeRequestResult<bool>> CancelAsync(int id, int userId)
    {
        var row = await repository.GetOwnedAsync(id, userId);
        if (row is null) return NotFound<bool>();
        if (row.Status != "Pending") return Conflict<bool>("Yêu cầu đã được xử lý, không thể hủy.");
        row.Status = "Cancelled";
        row.ResolvedAt = DateTime.UtcNow;
        row.ResolutionNote = "Đã hủy bởi người gửi.";
        await repository.SaveChangesAsync();
        return Success(true);
    }

    public async Task<CatalogChangeRequestResult<CatalogChangeRequest>> ApproveAsync(
        int id, int adminId, ResolveCatalogChangeRequest request)
    {
        await using var transaction = await repository.BeginTransactionAsync();
        var row = await repository.GetByIdAsync(id);
        if (row is null) return NotFound<CatalogChangeRequest>();
        if (row.Status != "Pending") return Conflict<CatalogChangeRequest>("Yêu cầu không còn ở trạng thái chờ duyệt.");
        var applyError = await ApplyAsync(row);
        if (applyError is not null) return Conflict<CatalogChangeRequest>("Không thể áp dụng yêu cầu.", applyError);

        row.Status = "Approved";
        row.ResolvedByUserId = adminId;
        row.ResolvedAt = DateTime.UtcNow;
        row.ResolutionNote = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();
        try
        {
            await repository.SaveChangesAsync();
            await transaction.CommitAsync();
            return Success(row);
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync();
            return Conflict<CatalogChangeRequest>(
                "Không thể áp dụng yêu cầu.", "Mã phụ tùng hoặc tên dịch vụ có thể đã được sử dụng.");
        }
    }

    public async Task<CatalogChangeRequestResult<CatalogChangeRequest>> RejectAsync(
        int id, int adminId, ResolveCatalogChangeRequest request)
    {
        var row = await repository.GetByIdAsync(id);
        if (row is null) return NotFound<CatalogChangeRequest>();
        if (row.Status != "Pending") return Conflict<CatalogChangeRequest>("Yêu cầu không còn ở trạng thái chờ duyệt.");
        if (string.IsNullOrWhiteSpace(request.Note)) return Invalid<CatalogChangeRequest>("Cần ghi lý do từ chối.");
        if (request.Note.Trim().Length > 1000)
            return Invalid<CatalogChangeRequest>("Lý do từ chối không được vượt quá 1.000 ký tự.");
        row.Status = "Rejected";
        row.ResolvedByUserId = adminId;
        row.ResolvedAt = DateTime.UtcNow;
        row.ResolutionNote = request.Note.Trim();
        await repository.SaveChangesAsync();
        return Success(row);
    }

    private async Task<string?> ValidatePayloadAsync(CreateCatalogChangeRequest request, string type, string action)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 150) return "Tên là bắt buộc và tối đa 150 ký tự.";
        if ((request.Description?.Length ?? 0) > 1000 || (request.WarrantyTerms?.Length ?? 0) > 1000) return "Mô tả hoặc điều kiện bảo hành vượt quá 1.000 ký tự.";
        if (request.WarrantyDuration is null or < 0 or > 36500 || request.WarrantyDurationUnit is not (1 or 2)) return "Thời hạn bảo hành phải từ 0 đến 36.500 ngày hoặc 120 tháng.";
        if (request.WarrantyDurationUnit == 2 && request.WarrantyDuration > 120) return "Thời hạn bảo hành tối đa là 120 tháng.";
        if (request.DeviceTypeId is null or <= 0 || !await repository.DeviceTypeExistsAsync(request.DeviceTypeId.Value)) return "Vui lòng chọn loại thiết bị hợp lệ.";
        if (type == "RepairService")
        {
            if (request.UnitPrice is null or < 0) return "Đơn giá dịch vụ không hợp lệ.";
            if (await repository.RepairServiceNameExistsAsync(request.Name!.Trim(), action == "Create" ? null : request.TargetId)) return "Tên dịch vụ đã tồn tại.";
        }
        else
        {
            if (string.IsNullOrWhiteSpace(request.PartNumber) || request.PartNumber.Trim().Length > 50) return "Mã phụ tùng là bắt buộc và tối đa 50 ký tự.";
            if (request.UnitCost is null or < 0 || request.SellingPrice is null or < 0 || request.ReorderLevel is null or < 0) return "Giá vốn, giá bán và mức tồn tối thiểu phải không âm.";
            if (await repository.SparePartNumberExistsAsync(request.PartNumber.Trim(), action == "Create" ? null : request.TargetId)) return "Mã phụ tùng đã tồn tại.";
        }
        return null;
    }

    private async Task<string?> ApplyAsync(CatalogChangeRequest row)
    {
        var payload = JsonSerializer.Deserialize<CreateCatalogChangeRequest>(row.PayloadJson, JsonOptions);
        if (row.Action == "Create" && payload is not null)
        {
            if (row.ItemType == "RepairService")
                repository.Add(new RepairService
                {
                    DeviceTypeId = payload.DeviceTypeId!.Value,
                    Name = payload.Name!.Trim(),
                    Description = payload.Description?.Trim(),
                    UnitPrice = payload.UnitPrice!.Value,
                    WarrantyDuration = payload.WarrantyDuration!.Value,
                    WarrantyDurationUnit = (WarrantyDurationUnit)payload.WarrantyDurationUnit!.Value,
                    WarrantyTerms = payload.WarrantyTerms?.Trim()
                });
            else
                repository.Add(new SparePart
                {
                    DeviceTypeId = payload.DeviceTypeId!.Value,
                    PartNumber = payload.PartNumber!.Trim(),
                    Name = payload.Name!.Trim(),
                    Description = payload.Description?.Trim(),
                    UnitCost = payload.UnitCost!.Value,
                    SellingPrice = payload.SellingPrice!.Value,
                    ReorderLevel = payload.ReorderLevel!.Value,
                    WarrantyDuration = payload.WarrantyDuration!.Value,
                    WarrantyDurationUnit = (WarrantyDurationUnit)payload.WarrantyDurationUnit!.Value,
                    WarrantyTerms = payload.WarrantyTerms?.Trim(),
                    QuantityInStock = 0
                });
            return null;
        }
        if (row.TargetId is null) return "Thiếu mã danh mục.";
        if (row.ItemType == "RepairService")
        {
            var item = await repository.GetRepairServiceAsync(row.TargetId.Value);
            if (item is null) return "Dịch vụ đã bị xóa.";
            if (row.Action == "Deactivate") item.IsActive = false;
            else if (payload is not null)
            {
                if (payload.DeviceTypeId.HasValue) item.DeviceTypeId = payload.DeviceTypeId.Value;
                item.Name = payload.Name!.Trim();
                item.Description = payload.Description?.Trim();
                item.UnitPrice = payload.UnitPrice!.Value;
                item.WarrantyDuration = payload.WarrantyDuration!.Value;
                item.WarrantyDurationUnit = (WarrantyDurationUnit)payload.WarrantyDurationUnit!.Value;
                item.WarrantyTerms = payload.WarrantyTerms?.Trim();
            }
        }
        else
        {
            var item = await repository.GetSparePartAsync(row.TargetId.Value);
            if (item is null) return "Phụ tùng đã bị xóa.";
            if (row.Action == "Deactivate") item.IsActive = false;
            else if (payload is not null)
            {
                if (payload.DeviceTypeId.HasValue) item.DeviceTypeId = payload.DeviceTypeId.Value;
                item.PartNumber = payload.PartNumber!.Trim();
                item.Name = payload.Name!.Trim();
                item.Description = payload.Description?.Trim();
                item.UnitCost = payload.UnitCost!.Value;
                item.SellingPrice = payload.SellingPrice!.Value;
                item.ReorderLevel = payload.ReorderLevel!.Value;
                item.WarrantyDuration = payload.WarrantyDuration!.Value;
                item.WarrantyDurationUnit = (WarrantyDurationUnit)payload.WarrantyDurationUnit!.Value;
                item.WarrantyTerms = payload.WarrantyTerms?.Trim();
            }
        }
        return null;
    }

    private static CatalogChangeRequestResult<T> Success<T>(T value) =>
        new(value, CatalogChangeRequestError.None);
    private static CatalogChangeRequestResult<T> Invalid<T>(string title, string? detail = null) =>
        new(default, CatalogChangeRequestError.Invalid, title, detail);
    private static CatalogChangeRequestResult<T> NotFound<T>(string? title = null) =>
        new(default, CatalogChangeRequestError.NotFound, title ?? "Không tìm thấy yêu cầu.");
    private static CatalogChangeRequestResult<T> Conflict<T>(string title, string? detail = null) =>
        new(default, CatalogChangeRequestError.Conflict, title, detail);
}
