using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using RepairCenter.DTOs.SpareParts;
using RepairCenter.Entities;
using RepairCenter.Services.Interfaces;

namespace RepairCenter.Controllers;

[ApiController]
[Route("api/spare-parts")]
[Authorize(Roles = "Admin,Receptionist,Technician")]
public class SparePartsController(ISparePartService service, IInventoryTransactionService transactionService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<SparePartResponse>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] bool activeOnly = true,
        [FromQuery] bool lowStockOnly = false)
    {
        var canManage = User.IsInRole("Admin");
        var parts = await service.GetAllAsync(search, canManage ? activeOnly : true, canManage && lowStockOnly);
        return Ok(parts.Select(part => ToResponse(part, canManage)));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SparePartResponse>> GetById(int id)
    {
        var part = await service.GetByIdAsync(id);
        return part is null ? NotFound() : Ok(ToResponse(part, User.IsInRole("Admin")));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<SparePartResponse>> Create(CreateSparePartRequest request)
    {
        if (!ValidWarrantyPolicy(request.WarrantyDuration, request.WarrantyDurationUnit))
            return BadRequest(new ProblemDetails { Title = "Thời hạn hoặc đơn vị bảo hành không hợp lệ." });
        var sparePart = new SparePart
        {
            DeviceTypeId = request.DeviceTypeId,
            PartNumber = request.PartNumber,
            Name = request.Name,
            Description = request.Description,
            UnitCost = request.UnitCost,
            SellingPrice = request.SellingPrice,
            ReorderLevel = request.ReorderLevel,
            WarrantyDuration = request.WarrantyDuration,
            WarrantyDurationUnit = request.WarrantyDurationUnit,
            WarrantyTerms = request.WarrantyTerms
        };

        var result = await service.CreateAsync(sparePart);
        if (result.Error != SparePartOperationError.None)
            return ToSparePartError(result.Error);

        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, ToResponse(result.Value, true));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<SparePartResponse>> Update(int id, UpdateSparePartRequest request)
    {
        if (!ValidWarrantyPolicy(request.WarrantyDuration, request.WarrantyDurationUnit))
            return BadRequest(new ProblemDetails { Title = "Thời hạn hoặc đơn vị bảo hành không hợp lệ." });
        var changes = new SparePart
        {
            DeviceTypeId = request.DeviceTypeId,
            PartNumber = request.PartNumber,
            Name = request.Name,
            Description = request.Description,
            UnitCost = request.UnitCost,
            SellingPrice = request.SellingPrice,
            ReorderLevel = request.ReorderLevel,
            IsActive = request.IsActive,
            WarrantyDuration = request.WarrantyDuration,
            WarrantyDurationUnit = request.WarrantyDurationUnit,
            WarrantyTerms = request.WarrantyTerms
        };

        var result = await service.UpdateAsync(id, changes);
        if (result.Error != SparePartOperationError.None)
            return ToSparePartError(result.Error);

        return Ok(ToResponse(result.Value!, true));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Deactivate(int id)
    {
        var result = await service.DeactivateAsync(id);
        return result.Error == SparePartOperationError.SparePartNotFound ? NotFound() : NoContent();
    }

    [HttpGet("{id:int}/transactions")]
    public async Task<ActionResult<IEnumerable<InventoryTransactionResponse>>> GetTransactions(int id)
    {
        if (await service.GetByIdAsync(id) is null)
            return NotFound();

        var result = await transactionService.GetAllAsync(id, null, null, null);

        return Ok(result.Value!.Select(transaction => ToTransactionResponse(transaction, User.IsInRole("Admin"))));
    }

    [HttpPost("{id:int}/transactions")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<InventoryTransactionResponse>> CreateTransaction(
        int id,
        CreateInventoryTransactionRequest request)
    {
        var result = await transactionService.CreateAsync(
            id,
            request.Type,
            request.Quantity,
            request.UnitCost,
            request.ReferenceNumber,
            request.Note);

        if (result.Error != InventoryTransactionError.None)
            return ToTransactionError(result.Error);

        return CreatedAtAction(
            nameof(GetTransactions),
            new { id },
            ToTransactionResponse(result.Value!, true));
    }

    private ActionResult<SparePartResponse> ToSparePartError(SparePartOperationError error) => error switch
    {
        SparePartOperationError.SparePartNotFound => NotFound(),
        SparePartOperationError.DuplicatePartNumber => Conflict(new ProblemDetails
        {
            Title = "Mã phụ tùng đã tồn tại",
            Detail = "PartNumber phải là duy nhất."
        }),
        SparePartOperationError.InvalidWarrantyPolicy => BadRequest(new ProblemDetails { Title = "Thời hạn bảo hành tối đa là 120 tháng hoặc 36.500 ngày." }),
        SparePartOperationError.InvalidDeviceType => BadRequest(new ProblemDetails { Title = "Vui lòng chọn loại thiết bị hợp lệ." }),
        _ => Problem(statusCode: StatusCodes.Status500InternalServerError)
    };

    private ActionResult<InventoryTransactionResponse> ToTransactionError(InventoryTransactionError error) => error switch
    {
        InventoryTransactionError.SparePartNotFound => NotFound(),
        InventoryTransactionError.InactiveSparePart => Conflict(new ProblemDetails
        {
            Title = "Phụ tùng đã ngừng hoạt động",
            Detail = "Không thể tạo giao dịch kho cho phụ tùng không hoạt động."
        }),
        InventoryTransactionError.InsufficientStock => Conflict(new ProblemDetails
        {
            Title = "Tồn kho không đủ",
            Detail = "Số lượng xuất kho vượt quá số lượng hiện có."
        }),
        _ => Problem(statusCode: StatusCodes.Status500InternalServerError)
    };

    private static SparePartResponse ToResponse(SparePart part, bool includeCost) => new()
    {
        Id = part.Id,
        DeviceTypeId = part.DeviceTypeId,
        DeviceTypeName = part.DeviceType?.Name,
        PartNumber = part.PartNumber,
        Name = part.Name,
        Description = part.Description,
        UnitCost = includeCost ? part.UnitCost : 0,
        SellingPrice = part.SellingPrice,
        QuantityInStock = part.QuantityInStock,
        ReorderLevel = part.ReorderLevel,
        IsLowStock = part.QuantityInStock <= part.ReorderLevel,
        IsActive = part.IsActive,
        WarrantyDuration = part.WarrantyDuration,
        WarrantyDurationUnit = part.WarrantyDurationUnit,
        WarrantyTerms = part.WarrantyTerms
    };

    private static bool ValidWarrantyPolicy(int duration, RepairCenter.Enums.WarrantyDurationUnit unit) => unit switch
    {
        RepairCenter.Enums.WarrantyDurationUnit.Days => duration is >= 0 and <= 36500,
        RepairCenter.Enums.WarrantyDurationUnit.Months => duration is >= 0 and <= 120,
        _ => false
    };

    private static InventoryTransactionResponse ToTransactionResponse(InventoryTransaction transaction, bool includeCost) => new()
    {
        Id = transaction.Id,
        SparePartId = transaction.SparePartId,
        PartNumber = transaction.SparePart.PartNumber,
        PartName = transaction.SparePart.Name,
        Type = transaction.TransactionType,
        Quantity = transaction.Quantity,
        UnitCost = includeCost ? transaction.UnitCost : 0,
        ReferenceNumber = transaction.ReferenceNumber,
        TransactionDate = transaction.TransactionDate,
        Note = transaction.Note
    };
}
