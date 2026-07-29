using Asp.Versioning;
using ClaudyGod.Application.Common.Models;
using ClaudyGod.Application.Features.Payments.Commands;
using ClaudyGod.Application.Features.Payments.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ClaudyGod.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/payments")]
public class PaymentController : ControllerBase
{
    private readonly IMediator _mediator;

    public PaymentController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Which payment methods are currently active. Lets the frontend hide/disable a
    /// method proactively instead of letting a user submit into a dead end.
    /// </summary>
    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    [HttpGet("status")]
    public async Task<ActionResult<ApiResponse<PaymentMethodsStatusDto>>> GetStatus(CancellationToken ct)
    {
        var status = await _mediator.Send(new GetPaymentMethodsStatusQuery(), ct);
        return Ok(ApiResponse<PaymentMethodsStatusDto>.Ok(status));
    }

    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("commerce")]
    [HttpPost("zelle/validate")]
    public async Task<ActionResult<ApiResponse<object>>> ValidateZelle(
        [FromBody] ValidateZelleRequest dto, CancellationToken ct)
    {
        var id = await _mediator.Send(new ValidateZellePaymentCommand(
            dto.TransactionId, dto.Amount, dto.SenderEmail,
            dto.SenderPhone, dto.Purpose, dto.OrderId), ct);

        return Ok(ApiResponse<object>.Ok(new { id }, "Zelle payment recorded and pending verification."));
    }

    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("commerce")]
    [HttpPost("ngn-transfer/validate")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<ApiResponse<object>>> ValidateNigerianTransfer(
        [FromForm] ValidateNgnTransferRequest dto, CancellationToken ct)
    {
        var id = await _mediator.Send(new ValidateNigerianTransferCommand(
            dto.Reference, dto.SenderName, dto.Amount, dto.Currency, dto.SlipFile), ct);

        return Ok(ApiResponse<object>.Ok(new { id }, "Bank transfer recorded and pending validation."));
    }

    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("commerce")]
    [HttpPost("paystack/record")]
    public async Task<ActionResult<ApiResponse<object>>> RecordPaystackPayment(
        [FromBody] RecordPaystackPaymentRequest dto, CancellationToken ct)
    {
        var id = await _mediator.Send(new RecordPaystackPaymentCommand(
            dto.DonorName, dto.DonorEmail, dto.Amount, dto.Currency, dto.Reference, dto.Message), ct);

        return Ok(ApiResponse<object>.Ok(new { id }, "Paystack payment recorded successfully."));
    }
}

public record ValidateZelleRequest(
    string TransactionId,
    decimal Amount,
    string? SenderEmail,
    string? SenderPhone,
    string? Purpose,
    Guid? OrderId);

public record ValidateNgnTransferRequest(
    string Reference,
    string SenderName,
    decimal Amount,
    string Currency,
    IFormFile SlipFile);

public record RecordPaystackPaymentRequest(
    string DonorName,
    string DonorEmail,
    decimal Amount,
    string Currency,
    string Reference,
    string? Message);
