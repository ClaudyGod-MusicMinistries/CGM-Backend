using ClaudyGod.Application.Common.Models;
using ClaudyGod.Application.Features.Events.Commands;
using ClaudyGod.Application.Features.Events.DTOs;
using ClaudyGod.Application.Features.Events.Queries;
using ClaudyGod.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClaudyGod.API.Controllers;

[ApiController]
[Route("api/events")]
public class EventController : ControllerBase
{
    private readonly IMediator _mediator;

    public EventController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedResult<EventDto>>>> GetAll(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10,
        [FromQuery] EventStatus? status = null, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetEventsQuery(page, pageSize, status), ct);
        return Ok(ApiResponse<PaginatedResult<EventDto>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<EventDetailDto>>> GetById(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetEventByIdQuery(id), ct);
        return Ok(ApiResponse<EventDetailDto>.Ok(result));
    }

    [Authorize(Roles = "Admin,SuperAdmin")]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<object>>> Create(
        [FromBody] CreateEventRequest dto, CancellationToken ct)
    {
        var id = await _mediator.Send(new CreateEventCommand(dto), ct);
        return CreatedAtAction(nameof(GetById), new { id },
            ApiResponse<object>.Ok(new { id }, "Event created."));
    }

    [Authorize(Roles = "Admin,SuperAdmin")]
    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<ApiResponse>> UpdateStatus(
        Guid id, [FromBody] UpdateEventStatusRequest dto, CancellationToken ct)
    {
        await _mediator.Send(new UpdateEventStatusCommand(id, dto.Status), ct);
        return Ok(ApiResponse.Ok("Event status updated."));
    }
}

public record UpdateEventStatusRequest(string Status);
