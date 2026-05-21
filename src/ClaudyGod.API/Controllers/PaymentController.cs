using ClaudyGod.Application.Common.Models;
using ClaudyGod.Application.Features.Payments.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ClaudyGod.API.Controllers;

[ApiController]
[Route("api/payments")]
public class PaymentController : ControllerBase
{
    private readonly IMediator _mediator;

    public PaymentController(IMediator mediator) => _mediator = mediator;

    [HttpPost("zelle/validate")]
    public async Task<ActionResult<ApiResponse<object>>> ValidateZelle(
        [FromBody] ValidateZelleRequest dto, CancellationToken ct)
    {
        var id = await _mediator.Send(new ValidateZellePaymentCommand(
            dto.TransactionId, dto.Amount, dto.SenderEmail,
            dto.SenderPhone, dto.Purpose, dto.OrderId), ct);

        return Ok(ApiResponse<object>.Ok(new { id }, "Zelle payment recorded and pending verification."));
    }

    [HttpPost("ngn-transfer/validate")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<ApiResponse<object>>> ValidateNigerianTransfer(
        [FromForm] ValidateNgnTransferRequest dto, CancellationToken ct)
    {
        var id = await _mediator.Send(new ValidateNigerianTransferCommand(
            dto.Reference, dto.SenderName, dto.Amount, dto.Currency, dto.SlipFile), ct);

        return Ok(ApiResponse<object>.Ok(new { id }, "Bank transfer recorded and pending validation."));
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
