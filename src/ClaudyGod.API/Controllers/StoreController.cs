using Asp.Versioning;
using ClaudyGod.Application.Common.Models;
using ClaudyGod.Application.Features.Store.Commands;
using ClaudyGod.Application.Features.Store.DTOs;
using ClaudyGod.Application.Features.Store.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClaudyGod.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/store")]
public class StoreController : ControllerBase
{
    private readonly IMediator _mediator;

    public StoreController(IMediator mediator) => _mediator = mediator;

    [HttpGet("products")]
    public async Task<ActionResult<ApiResponse<List<ProductDto>>>> GetProducts(
        [FromQuery] string? category, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetProductsQuery(category), ct);
        return Ok(ApiResponse<List<ProductDto>>.Ok(result));
    }

    [Authorize(Roles = "Admin,SuperAdmin")]
    [HttpPost("products")]
    public async Task<ActionResult<ApiResponse<object>>> CreateProduct(
        [FromBody] CreateProductRequest dto, CancellationToken ct)
    {
        var id = await _mediator.Send(new CreateProductCommand(dto), ct);
        return CreatedAtAction(nameof(GetProducts), ApiResponse<object>.Ok(new { id }, "Product created."));
    }

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
