using Asp.Versioning;
using ClaudyGod.Application.Common.Models;
using ClaudyGod.Application.Features.Tickets.Commands;
using ClaudyGod.Application.Features.Tickets.DTOs;
using ClaudyGod.Application.Features.Tickets.Queries;
using ClaudyGod.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClaudyGod.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/tickets")]
public class TicketController : ControllerBase
{
    private readonly IMediator _mediator;

    public TicketController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    public async Task<ActionResult<ApiResponse<object>>> Reserve(
        [FromBody] ReserveTicketRequest dto, CancellationToken ct)
    {
        var confirmationCode = await _mediator.Send(new ReserveTicketCommand(dto), ct);
        return Ok(ApiResponse<object>.Ok(new { confirmationCode }, "Ticket reserved successfully."));
    }

    [Authorize(Roles = "Admin,SuperAdmin")]
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedResult<TicketDto>>>> GetAll(
        [FromQuery] Guid? eventId = null, [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20, [FromQuery] TicketStatus? status = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetTicketsQuery(eventId, page, pageSize, status), ct);
        return Ok(ApiResponse<PaginatedResult<TicketDto>>.Ok(result));
    }
}
