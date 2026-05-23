using Asp.Versioning;
using ClaudyGod.Application.Common.Models;
using ClaudyGod.Application.Features.Subscribers.Commands;
using ClaudyGod.Application.Features.Subscribers.DTOs;
using ClaudyGod.Application.Features.Subscribers.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClaudyGod.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/subscribers")]
public class SubscriberController : ControllerBase
{
    private readonly IMediator _mediator;

    public SubscriberController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    public async Task<ActionResult<ApiResponse<object>>> Subscribe(
        [FromBody] SubscribeRequestDto dto, CancellationToken ct)
    {
        var id = await _mediator.Send(new SubscribeCommand(dto.Name, dto.Email), ct);
        return Ok(ApiResponse<object>.Ok(new { id }, "Successfully subscribed!"));
    }

    [HttpDelete("unsubscribe")]
    public async Task<ActionResult<ApiResponse>> Unsubscribe(
        [FromQuery] string email, [FromQuery] string token, CancellationToken ct)
    {
        await _mediator.Send(new UnsubscribeCommand(email, token), ct);
        return Ok(ApiResponse.Ok("You have been unsubscribed."));
    }

    [Authorize(Roles = "Admin,SuperAdmin")]
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedResult<SubscriberDto>>>> GetAll(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] bool? isActive = null, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetSubscribersQuery(page, pageSize, isActive), ct);
        return Ok(ApiResponse<PaginatedResult<SubscriberDto>>.Ok(result));
    }
}
