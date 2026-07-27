using Asp.Versioning;
using ClaudyGod.API.Attributes;
using ClaudyGod.Application.Common.Models;
using ClaudyGod.Application.Features.Media.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ClaudyGod.API.Controllers;

/// <summary>
/// YouTube secure proxy endpoints. Prevents direct YouTube link exposure.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/media/youtube")]
[PublicEndpoint]
public class YoutubeController : ControllerBase
{
    private readonly IMediator _mediator;

    public YoutubeController(IMediator mediator) => _mediator = mediator;

    /// <summary>GET /api/v1.0/media/youtube/{videoId} — get a secure YouTube embed URL.</summary>
    [HttpGet("{videoId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ApiResponse<YoutubeEmbedDto>>> GetEmbedUrl(
        [FromRoute] string videoId,
        [FromQuery] bool autoplay = false,
        [FromQuery] bool controls = true,
        [FromQuery] bool modestBranding = true,
        CancellationToken ct = default)
    {
        var response = await _mediator.Send(
            new GetYoutubeEmbedUrlQuery(videoId, autoplay, controls, modestBranding), ct);

        return Ok(ApiResponse<YoutubeEmbedDto>.Ok(response, "YouTube embed URL generated successfully"));
    }

    /// <summary>POST /api/v1.0/media/youtube/{videoId} — get embed URL with custom options.</summary>
    [HttpPost("{videoId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ApiResponse<YoutubeEmbedDto>>> GenerateEmbedUrl(
        [FromRoute] string videoId,
        [FromBody] YoutubeEmbedRequest? request,
        CancellationToken ct)
    {
        var response = await _mediator.Send(
            new GetYoutubeEmbedUrlQuery(
                videoId,
                request?.Autoplay ?? false,
                request?.Controls ?? true,
                request?.ModestBranding ?? true), ct);

        return Ok(ApiResponse<YoutubeEmbedDto>.Ok(response, "YouTube embed URL generated"));
    }
}

/// <summary>Request model for YouTube embed customization.</summary>
public class YoutubeEmbedRequest
{
    public bool? Autoplay { get; set; }
    public bool? Controls { get; set; }
    public bool? ModestBranding { get; set; }
}
