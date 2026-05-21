using ClaudyGod.Application.Common.Models;
using ClaudyGod.Application.Features.Volunteers.Commands;
using ClaudyGod.Application.Features.Volunteers.DTOs;
using ClaudyGod.Application.Features.Volunteers.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClaudyGod.API.Controllers;

[ApiController]
[Route("api/volunteers")]
public class VolunteerController : ControllerBase
{
    private readonly IMediator _mediator;

    public VolunteerController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    public async Task<ActionResult<ApiResponse<object>>> Register(
        [FromBody] RegisterVolunteerRequest dto, CancellationToken ct)
    {
        var id = await _mediator.Send(new RegisterVolunteerCommand(dto), ct);
        return Ok(ApiResponse<object>.Ok(new { id }, "Volunteer registration submitted."));
    }

    [Authorize(Roles = "Admin,SuperAdmin")]
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedResult<VolunteerDto>>>> GetAll(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] bool? isApproved = null, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetVolunteersQuery(page, pageSize, isApproved), ct);
        return Ok(ApiResponse<PaginatedResult<VolunteerDto>>.Ok(result));
    }
}
