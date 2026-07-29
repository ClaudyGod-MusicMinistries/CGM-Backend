using Asp.Versioning;
using ClaudyGod.API.Attributes;
using ClaudyGod.Application.Common.Models;
using ClaudyGod.Application.Features.Store.Commands;
using ClaudyGod.Application.Features.Store.DTOs;
using ClaudyGod.Application.Features.Store.Queries;
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

    [PublicEndpoint]
    [HttpGet("products")]
    public async Task<ActionResult<ApiResponse<List<ProductDto>>>> GetProducts(
        [FromQuery] string? category, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetProductsQuery(category), ct);
        return Ok(ApiResponse<List<ProductDto>>.Ok(result));
    }

    [HttpPost("products")]
    public async Task<ActionResult<ApiResponse<object>>> CreateProduct(
        [FromBody] CreateProductRequest dto, CancellationToken ct)
    {
        var id = await _mediator.Send(new CreateProductCommand(dto), ct);
        return CreatedAtAction(nameof(GetProducts), ApiResponse<object>.Ok(new { id }, "Product created."));
    }

    [HttpPut("products/{id:guid}")]
    public async Task<ActionResult<ApiResponse>> UpdateProduct(
        Guid id, [FromBody] CreateProductRequest dto, CancellationToken ct)
    {
        await _mediator.Send(new UpdateProductCommand(id, dto), ct);
        return Ok(ApiResponse.Ok("Product updated."));
    }

    [HttpDelete("products/{id:guid}")]
    public async Task<ActionResult<ApiResponse>> DeleteProduct(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new DeleteProductCommand(id), ct);
        return Ok(ApiResponse.Ok("Product deleted."));
    }

    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("commerce")]
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
