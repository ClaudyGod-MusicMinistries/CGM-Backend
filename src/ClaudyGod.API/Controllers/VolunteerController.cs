using Asp.Versioning;
using ClaudyGod.API.Attributes;
using ClaudyGod.Application.Common.Models;
using ClaudyGod.Application.Features.Volunteers.Commands;
using ClaudyGod.Application.Features.Volunteers.DTOs;
using ClaudyGod.Application.Features.Volunteers.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ClaudyGod.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/volunteers")]
public class VolunteerController : ControllerBase
{
    private readonly IMediator _mediator;

    public VolunteerController(IMediator mediator) => _mediator = mediator;

    [PublicEndpoint]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("public-form")]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<object>>> Register(
        [FromBody] RegisterVolunteerRequest dto, CancellationToken ct)
    {
        var id = await _mediator.Send(new RegisterVolunteerCommand(dto), ct);
        return Ok(ApiResponse<object>.Ok(new { id }, "Volunteer registration submitted."));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedResult<VolunteerDto>>>> GetAll(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] bool? isApproved = null, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetVolunteersQuery(page, pageSize, isApproved), ct);
        return Ok(ApiResponse<PaginatedResult<VolunteerDto>>.Ok(result));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new DeleteVolunteerCommand(id), ct);
        return Ok(ApiResponse.Ok("Volunteer application moved to trash."));
    }
}
