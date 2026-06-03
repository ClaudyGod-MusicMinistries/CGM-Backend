using Asp.Versioning;
using ClaudyGod.Application.Common.Models;
using ClaudyGod.Application.Features.Store.Commands;
using ClaudyGod.Application.Features.Store.DTOs;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ClaudyGod.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/store")]
public class StoreController : ControllerBase
{
    private readonly IMediator _mediator;

    public StoreController(IMediator mediator) => _mediator = mediator;

    [HttpPost("checkout")]
    public async Task<ActionResult<ApiResponse<object>>> Checkout(
        [FromBody] CreateOrderRequest request, CancellationToken ct)
    {
        var orderId = await _mediator.Send(new CreateOrderCommand(request), ct);
        return Ok(ApiResponse<object>.Ok(
            new { id = orderId, message = "Order created successfully" },
            "Order received successfully."));
    }
}
