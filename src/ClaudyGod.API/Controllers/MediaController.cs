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
        [FromQuery] string? type = null,
        [FromQuery] bool? isPublished = null,
        CancellationToken ct = default)
    {
        var mediaType = ParseMediaType(type);
        var result = await _mediator.Send(new GetMediaQuery(page, pageSize, mediaType, isPublished), ct);
        return Ok(ApiResponse<PaginatedResult<MediaItemDto>>.Ok(result));
    }

    private static MediaType? ParseMediaType(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var normalized = value.Trim().Replace("-", string.Empty).Replace("_", string.Empty);
        if (normalized.Equals("video", StringComparison.OrdinalIgnoreCase))
            return MediaType.SermonVideo;
        if (normalized.Equals("audio", StringComparison.OrdinalIgnoreCase))
            return MediaType.SermonAudio;

        if (Enum.TryParse<MediaType>(normalized, true, out var mediaType) &&
            Enum.IsDefined(mediaType))
            return mediaType;

        throw new ClaudyGod.Domain.Exceptions.ValidationException(
            new Dictionary<string, string[]>
            {
                ["type"] = ["Choose one of: video, audio, music, photo, or other."]
            });
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
}
