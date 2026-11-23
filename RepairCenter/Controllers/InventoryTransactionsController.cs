using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using RepairCenter.DTOs.InventoryTransactions;
using RepairCenter.DTOs.SpareParts;
using RepairCenter.Entities;
using RepairCenter.Services.Interfaces;

namespace RepairCenter.Controllers;

[ApiController]
[Route("api/inventory-transactions")]
[Authorize(Roles = "Admin,Receptionist,Technician")]
public class InventoryTransactionsController(IInventoryTransactionService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<InventoryTransactionResponse>>> GetAll(
        [FromQuery] int? sparePartId,
        [FromQuery] string? type,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        var result = await service.GetAllAsync(sparePartId, type, from, to);
        if (result.Error == InventoryTransactionError.InvalidDateRange)
            return BadRequest(new ProblemDetails { Title = "Khoảng thời gian không hợp lệ", Detail = "'from' phải nhỏ hơn hoặc bằng 'to'." });
        if (result.Error == InventoryTransactionError.InvalidType)
            return BadRequest(new ProblemDetails { Title = "Loại giao dịch không hợp lệ", Detail = "Type chỉ nhận StockIn hoặc StockOut." });

        return Ok(result.Value!.Select(transaction => ToResponse(transaction, User.IsInRole("Admin"))));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<InventoryTransactionResponse>> GetById(int id)
    {
        var transaction = await service.GetByIdAsync(id);
        return transaction is null ? NotFound() : Ok(ToResponse(transaction, User.IsInRole("Admin")));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<InventoryTransactionResponse>> Create(
        RepairCenter.DTOs.InventoryTransactions.CreateInventoryTransactionRequest request)
    {
        var result = await service.CreateAsync(
            request.SparePartId,
            request.Type,
            request.Quantity,
            request.UnitCost,
            request.ReferenceNumber,
            request.Note);

        if (result.Error != InventoryTransactionError.None)
            return ToError(result.Error);

        var created = result.Value!;
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, ToResponse(created, true));
    }

    private ActionResult<InventoryTransactionResponse> ToError(InventoryTransactionError error) => error switch
    {
        InventoryTransactionError.SparePartNotFound => NotFound(new ProblemDetails
        {
            Title = "Không tìm thấy phụ tùng",
            Detail = "SparePartId không tồn tại."
        }),
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
        InventoryTransactionError.InvalidType or InventoryTransactionError.InvalidQuantity or InventoryTransactionError.InvalidUnitCost =>
            BadRequest(new ProblemDetails { Title = "Dữ liệu giao dịch không hợp lệ" }),
        _ => Problem(statusCode: StatusCodes.Status500InternalServerError)
    };

    internal static InventoryTransactionResponse ToResponse(InventoryTransaction transaction, bool includeCost) => new()
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
