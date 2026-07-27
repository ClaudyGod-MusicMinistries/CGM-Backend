using Asp.Versioning;
using ClaudyGod.Application.Common.Models;
using ClaudyGod.Application.Features.Albums.Commands;
using ClaudyGod.Application.Features.Albums.DTOs;
using ClaudyGod.Application.Features.Albums.Queries;
using MediatR;
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
    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<AlbumDto>>>> GetAll(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetAlbumsQuery(), ct);
        return Ok(ApiResponse<List<AlbumDto>>.Ok(result));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<object>>> Create(
        [FromBody] CreateAlbumRequest dto, CancellationToken ct)
    {
        var id = await _mediator.Send(new CreateAlbumCommand(dto), ct);
        return CreatedAtAction(nameof(GetAll), ApiResponse<object>.Ok(new { id }, "Album created."));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse>> Update(
        Guid id, [FromBody] CreateAlbumRequest dto, CancellationToken ct)
    {
        await _mediator.Send(new UpdateAlbumCommand(id, dto), ct);
        return Ok(ApiResponse.Ok("Album updated."));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new DeleteAlbumCommand(id), ct);
        return Ok(ApiResponse.Ok("Album deleted."));
    }
}
