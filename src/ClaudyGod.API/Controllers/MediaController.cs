using Asp.Versioning;
using ClaudyGod.Application.Common.Models;
using ClaudyGod.Application.Features.Media.Commands;
using ClaudyGod.Application.Features.Media.DTOs;
using ClaudyGod.Application.Features.Media.Queries;
using ClaudyGod.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClaudyGod.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/media")]
public class MediaController : ControllerBase
{
    private readonly IMediator _mediator;

    public MediaController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedResult<MediaItemDto>>>> GetAll(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] MediaType? type = null,
        [FromQuery] bool? isPublished = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetMediaQuery(page, pageSize, type, isPublished), ct);
        return Ok(ApiResponse<PaginatedResult<MediaItemDto>>.Ok(result));
    }

    [Authorize(Roles = "Admin,SuperAdmin")]
    [HttpPost]
    [RequestSizeLimit(500 * 1024 * 1024)]
    public async Task<ActionResult<ApiResponse<object>>> Upload(
        [FromForm] UploadMediaRequest dto, CancellationToken ct)
    {
        var id = await _mediator.Send(new UploadMediaCommand(dto), ct);
        return Ok(ApiResponse<object>.Ok(new { id }, "Media uploaded successfully."));
    }

    /// <summary>
    /// Registers externally-hosted media (a YouTube link, etc.) with no file
    /// upload — for video content that lives on YouTube rather than in our
    /// own storage. See <see cref="Upload"/> for real file uploads.
    /// </summary>
    [Authorize(Roles = "Admin,SuperAdmin")]
    [HttpPost("link")]
    public async Task<ActionResult<ApiResponse<object>>> CreateLink(
        [FromBody] CreateMediaLinkRequest dto, CancellationToken ct)
    {
        var id = await _mediator.Send(new CreateMediaLinkCommand(dto), ct);
        return Ok(ApiResponse<object>.Ok(new { id }, "Media link created successfully."));
    }
}
