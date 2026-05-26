using Asp.Versioning;
using ClaudyGod.Application.Common.Models;
using ClaudyGod.Application.Features.Reels.DTOs;
using ClaudyGod.Application.Features.Reels.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ClaudyGod.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/reels")]
public class ReelController : ControllerBase
{
    private readonly IMediator _mediator;

    public ReelController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Returns published reels (YouTube videos), optionally filtered by category.
    /// Categories: featured, sermon, teaching, music_video, live, christmas
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<ReelDto>>>> GetAll(
        [FromQuery] string? category,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetReelsQuery(category, page, pageSize), ct);
        return Ok(ApiResponse<List<ReelDto>>.Ok(result));
    }
}
