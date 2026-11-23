using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using RepairCenter.DTOs.Invoices;
using RepairCenter.Entities;
using RepairCenter.Enums;
using RepairCenter.Repositories.Interfaces;
using RepairCenter.Services.Interfaces;
using System.Security.Claims;

namespace RepairCenter.Controllers;

[ApiController]
[Route("api/invoices")]
[Authorize(Roles = "Admin,Receptionist")]
public class InvoicesController(IBillingService service) : ControllerBase
{
    [HttpGet("{id:int}")]
    public async Task<ActionResult<InvoiceResponse>> GetById(int id)
    {
        var invoice = await service.GetInvoiceAsync(id);
        return invoice is null ? NotFound() : Ok(ToResponse(invoice));
    }

    [HttpGet("by-ticket/{ticketId:int}")]
    public async Task<ActionResult<InvoiceResponse>> GetByTicket(int ticketId)
    {
        var invoice = await service.GetInvoiceByTicketAsync(ticketId);
        return invoice is null ? NotFound() : Ok(ToResponse(invoice));
    }

    [HttpPost("by-ticket/{ticketId:int}")]
    public async Task<ActionResult<InvoiceResponse>> Create(int ticketId, CreateInvoiceRequest request)
    {
        var result = await service.CreateInvoiceAsync(ticketId, request.DiscountPercent, request.TaxPercent, request.DueAt, request.Notes);
        if (result.Error != BillingError.None) return Error(result.Error);
        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, ToResponse(result.Value));
    }

    [HttpPost("{id:int}/payments")]
    public async Task<ActionResult<InvoicePaymentResponse>> RecordPayment(int id, RecordInvoicePaymentRequest request)
    {
        if (!TryGetProcessorId(out var processorId)) return Unauthorized();
        var result = await service.RecordPaymentAsync(id, request.Amount, request.PaymentMethod,
            request.ReferenceNumber, request.Notes, processorId);
        if (result.Error != BillingError.None) return Error(result.Error);
        return Ok(ToPaymentResponse(result.Value!, User.FindFirstValue(ClaimTypes.GivenName)));
    }

    [HttpPost("{id:int}/refunds")]
    public async Task<ActionResult<InvoicePaymentResponse>> Refund(int id, RefundInvoicePaymentRequest request)
    {
        if (!TryGetProcessorId(out var processorId)) return Unauthorized();
        var result = await service.RefundPaymentAsync(id, request.PaymentId, request.Amount,
            request.RefundMethod, request.Reason, processorId);
        if (result.Error != BillingError.None) return Error(result.Error);
        return Ok(ToPaymentResponse(result.Value!, User.FindFirstValue(ClaimTypes.GivenName)));
    }

    [HttpPost("{id:int}/payments/{paymentId:int}/void")]
    public async Task<ActionResult<InvoicePaymentResponse>> VoidPayment(
        int id, int paymentId, VoidInvoicePaymentRequest request)
    {
        if (!TryGetProcessorId(out var processorId)) return Unauthorized();
        var result = await service.VoidPaymentAsync(id, paymentId, request.RefundMethod,
            request.Reason, processorId);
        if (result.Error != BillingError.None) return Error(result.Error);
        return Ok(ToPaymentResponse(result.Value!, User.FindFirstValue(ClaimTypes.GivenName)));
    }

    private ActionResult Error(BillingError error) => error switch
    {
        BillingError.TicketNotFound => NotFound(new ProblemDetails { Title = "Không tìm thấy phiếu sửa chữa." }),
        BillingError.InvoiceNotFound => NotFound(new ProblemDetails { Title = "Không tìm thấy hóa đơn." }),
        BillingError.TicketNotCompleted => Conflict(new ProblemDetails { Title = "Chỉ tạo hóa đơn khi phiếu đã hoàn tất." }),
        BillingError.TicketHasNoDetails => Conflict(new ProblemDetails { Title = "Phiếu chưa có hạng mục để tính tiền.", Detail = "Hãy thêm ít nhất một dịch vụ hoặc phụ tùng vào phiếu trước khi phát hành hóa đơn." }),
        BillingError.InvoiceAlreadyExists => Conflict(new ProblemDetails { Title = "Phiếu đã có hóa đơn." }),
        BillingError.InvalidAmount => BadRequest(new ProblemDetails { Title = "Số tiền không hợp lệ." }),
        BillingError.Overpayment => Conflict(new ProblemDetails { Title = "Số tiền thanh toán vượt quá số dư hóa đơn." }),
        BillingError.InvoiceNotIssued => Conflict(new ProblemDetails { Title = "Hóa đơn không còn khoản phải thu." }),
        BillingError.InvalidProcessor => Unauthorized(new ProblemDetails { Title = "Tài khoản thu/hoàn tiền không còn hoạt động." }),
        BillingError.PaymentNotFound => NotFound(new ProblemDetails { Title = "Không tìm thấy giao dịch thanh toán thuộc hóa đơn này." }),
        BillingError.PaymentNotRefundable => Conflict(new ProblemDetails { Title = "Giao dịch đã được hoàn tiền hoặc hủy toàn bộ." }),
        BillingError.RefundExceedsAvailable => Conflict(new ProblemDetails { Title = "Số tiền hoàn vượt quá số tiền còn có thể hoàn." }),
        _ => Problem(statusCode: StatusCodes.Status500InternalServerError)
    };

    private static InvoiceResponse ToResponse(Invoice invoice) => new()
    {
        Id = invoice.Id, InvoiceNumber = invoice.InvoiceNumber,
        RepairTicketId = invoice.RepairTicketId, TicketNumber = invoice.RepairTicket.TicketNumber,
        IssuedAt = invoice.IssuedAt, DueAt = invoice.DueAt, Subtotal = invoice.Subtotal,
        DiscountPercent = invoice.DiscountPercent, DiscountAmount = invoice.DiscountAmount,
        TaxPercent = invoice.TaxPercent, TaxAmount = invoice.TaxAmount, TotalAmount = invoice.TotalAmount, AmountPaid = invoice.AmountPaid,
        PaymentStatus = CurrentPaymentStatus(invoice), PaidAt = invoice.PaidAt, Notes = invoice.Notes,
        Payments = invoice.Payments.OrderBy(x => x.PaidAt).Select(x => ToPaymentResponse(x)).ToList()
    };

    private static PaymentStatus CurrentPaymentStatus(Invoice invoice)
    {
        if (invoice.AmountPaid >= invoice.TotalAmount) return PaymentStatus.Paid;
        if (invoice.DueAt.HasValue && invoice.DueAt.Value <= DateTime.UtcNow)
            return PaymentStatus.Overdue;
        return invoice.AmountPaid > 0 ? PaymentStatus.PartiallyPaid : PaymentStatus.Pending;
    }

    private bool TryGetProcessorId(out int processorId) =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out processorId);

    private static InvoicePaymentResponse ToPaymentResponse(InvoicePayment payment, string? processorName = null) => new()
    {
        Id = payment.Id, Amount = payment.Amount, Type = payment.Type.ToString(),
        ProcessedByUserId = payment.ProcessedByUserId,
        ProcessedByUserName = payment.ProcessedByUser?.FullName ?? processorName,
        RefundOfPaymentId = payment.RefundOfPaymentId,
        PaymentMethod = payment.PaymentMethod, ReferenceNumber = payment.ReferenceNumber,
        PaidAt = payment.PaidAt, Notes = payment.Notes
    };
}
