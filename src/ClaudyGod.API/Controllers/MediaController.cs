using Asp.Versioning;
using ClaudyGod.Application.Common.Models;
using ClaudyGod.Application.Features.Media.Commands;
using ClaudyGod.Application.Features.Media.DTOs;
using ClaudyGod.Application.Features.Media.Queries;
using ClaudyGod.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ClaudyGod.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/media")]
public class MediaController : ControllerBase
{
    private readonly IMediator _mediator;

    public MediaController(IMediator mediator) => _mediator = mediator;

    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedResult<MediaItemDto>>>> GetAll(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? type = null,
        [FromQuery] bool? isPublished = null,
        CancellationToken ct = default)
    {
        MediaType? parsedType = null;
        if (!string.IsNullOrEmpty(type))
        {
            if (!Enum.TryParse<MediaType>(type, ignoreCase: true, out var value))
            {
                var valid = string.Join(", ", Enum.GetNames<MediaType>());
                throw new ClaudyGod.Domain.Exceptions.ValidationException(
                    new Dictionary<string, string[]> { ["type"] = [$"Type must be one of: {valid}."] });
            }
            parsedType = value;
        }

        var result = await _mediator.Send(new GetMediaQuery(page, pageSize, parsedType, isPublished), ct);
        return Ok(ApiResponse<PaginatedResult<MediaItemDto>>.Ok(result));
    }

    /// <summary>
    /// Creates a MediaItem from an already-confirmed upload session — the file
    /// bytes landed in S3 during StorageController's confirm step, not here.
    /// See CreateMediaFromUploadCommand.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<object>>> Create(
        [FromBody] CreateMediaFromUploadRequest dto, CancellationToken ct)
    {
        var id = await _mediator.Send(new CreateMediaFromUploadCommand(dto), ct);
        return Ok(ApiResponse<object>.Ok(new { id }, "Media created successfully."));
    }

    /// <summary>
    /// Registers externally-hosted media (a YouTube link, etc.) with no file
    /// upload — for video content that lives on YouTube rather than in our
    /// own storage. See <see cref="Upload"/> for real file uploads.
    /// </summary>
    [HttpPost("link")]
    public async Task<ActionResult<ApiResponse<object>>> CreateLink(
        [FromBody] CreateMediaLinkRequest dto, CancellationToken ct)
    {
        var id = await _mediator.Send(new CreateMediaLinkCommand(dto), ct);
        return Ok(ApiResponse<object>.Ok(new { id }, "Media link created successfully."));
    }

    /// <summary>
    /// Edits a link-created item's metadata. Real uploaded files have no
    /// update path — their binary content is immutable, only Delete applies.
    /// </summary>
    [HttpPut("link/{id:guid}")]
    public async Task<ActionResult<ApiResponse>> UpdateLink(
        Guid id, [FromBody] CreateMediaLinkRequest dto, CancellationToken ct)
    {
        await _mediator.Send(new UpdateMediaLinkCommand(id, dto), ct);
        return Ok(ApiResponse.Ok("Media link updated."));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new DeleteMediaCommand(id), ct);
        return Ok(ApiResponse.Ok("Media item deleted."));
    }
}
