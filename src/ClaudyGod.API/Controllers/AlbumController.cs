using Asp.Versioning;
using ClaudyGod.Application.Common.Models;
using ClaudyGod.Application.Features.Albums.Commands;
using ClaudyGod.Application.Features.Albums.DTOs;
using ClaudyGod.Application.Features.Albums.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClaudyGod.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/albums")]
public class AlbumController : ControllerBase
{
    private readonly IMediator _mediator;

    public AlbumController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Returns all published music albums with streaming platform links.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<AlbumDto>>>> GetAll(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetAlbumsQuery(), ct);
        return Ok(ApiResponse<List<AlbumDto>>.Ok(result));
    }

    [Authorize(Roles = "Admin,SuperAdmin")]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<object>>> Create(
        [FromBody] CreateAlbumRequest dto, CancellationToken ct)
    {
        var id = await _mediator.Send(new CreateAlbumCommand(dto), ct);
        return CreatedAtAction(nameof(GetAll), ApiResponse<object>.Ok(new { id }, "Album created."));
    }
}
