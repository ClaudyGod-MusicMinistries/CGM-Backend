using Asp.Versioning;
using ClaudyGod.API.Attributes;
using ClaudyGod.Application.Common.Models;
using ClaudyGod.Application.Features.Subscribers.Commands;
using ClaudyGod.Application.Features.Subscribers.DTOs;
using ClaudyGod.Application.Features.Subscribers.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClaudyGod.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/subscribers")]
public class SubscriberController : ControllerBase
{
    private readonly IMediator _mediator;

    public SubscriberController(IMediator mediator) => _mediator = mediator;

    [PublicEndpoint]
    [HttpPost]
    [EnableRateLimiting("subscription")]
    public async Task<ActionResult<ApiResponse<object>>> Subscribe(
        [FromBody] SubscribeRequestDto dto, CancellationToken ct)
    {
        var id = await _mediator.Send(new SubscribeCommand(dto.Name, dto.Email), ct);
        return Ok(ApiResponse<object>.Ok(new { id }, "Successfully subscribed!"));
    }

    [PublicEndpoint]
    [HttpDelete("unsubscribe")]
    [EnableRateLimiting("subscription")]
    public async Task<ActionResult<ApiResponse>> Unsubscribe(
        [FromQuery] string email, [FromQuery] string token, CancellationToken ct)
    {
        await _mediator.Send(new UnsubscribeCommand(email, token), ct);
        return Ok(ApiResponse.Ok("You have been unsubscribed."));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedResult<SubscriberDto>>>> GetAll(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] bool? isActive = null, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetSubscribersQuery(page, pageSize, isActive), ct);
        return Ok(ApiResponse<PaginatedResult<SubscriberDto>>.Ok(result));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new DeleteSubscriberCommand(id), ct);
        return Ok(ApiResponse.Ok("Subscriber moved to trash."));
    }
}
