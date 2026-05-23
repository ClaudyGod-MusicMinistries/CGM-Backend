using ClaudyGod.Application.Common.Models;
using ClaudyGod.Application.Features.Admin.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClaudyGod.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class AdminController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminController(IMediator mediator) => _mediator = mediator;

    [HttpGet("dashboard")]
    public async Task<ActionResult<ApiResponse<DashboardStatsDto>>> GetDashboard(CancellationToken ct)
    {
        var stats = await _mediator.Send(new GetDashboardStatsQuery(), ct);
        return Ok(ApiResponse<DashboardStatsDto>.Ok(stats));
    }
}
