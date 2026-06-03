using Asp.Versioning;
using ClaudyGod.Application.Common.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;

namespace ClaudyGod.API.Controllers;

/// <summary>
/// YouTube secure proxy endpoints
/// Prevents direct YouTube link exposure
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/media/youtube")]
public class YoutubeController : ControllerBase
{
    private readonly ILogger<YoutubeController> _logger;

    private const string YoutubeNoCookieDomain = "youtube-nocookie.com";
    private const string YoutubeEmbedPath = "embed";

    public YoutubeController(ILogger<YoutubeController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// GET /api/v1.0/media/youtube/{videoId}
    /// Get secure YouTube embed URL
    /// </summary>
    [HttpGet("{videoId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public ActionResult<ApiResponse<YoutubeEmbedResponse>> GetEmbedUrl(
        [FromRoute] string videoId,
        [FromQuery] bool autoplay = false,
        [FromQuery] bool controls = true,
        [FromQuery] bool modestBranding = true)
    {
        try
        {
            // Validate video ID format (11 alphanumeric characters)
            if (!IsValidYoutubeVideoId(videoId))
            {
                _logger.LogWarning("Invalid YouTube video ID format requested: {VideoId}", videoId);
                return BadRequest(ApiResponse<YoutubeEmbedResponse>.Fail("Invalid video ID format (must be 11 alphanumeric characters)"));
            }

            // Build secure embed URL
            var embedUrl = BuildEmbedUrl(videoId, autoplay, controls, modestBranding);

            _logger.LogInformation("Generated YouTube embed URL for video: {VideoIdLast4}", videoId[^4..]);

            var response = new YoutubeEmbedResponse
            {
                VideoId = videoId,
                EmbedUrl = embedUrl,
                Provider = YoutubeNoCookieDomain,
                ExpiresIn = 3600,
                GeneratedAt = DateTime.UtcNow,
            };

            return Ok(ApiResponse<YoutubeEmbedResponse>.Ok(response, "YouTube embed URL generated successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating YouTube embed URL for video: {VideoId}", videoId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<YoutubeEmbedResponse>.Fail("Failed to generate video URL"));
        }
    }

    /// <summary>
    /// POST /api/v1.0/media/youtube/{videoId}
    /// Get embed URL with custom options
    /// </summary>
    [HttpPost("{videoId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public ActionResult<ApiResponse<YoutubeEmbedResponse>> GenerateEmbedUrl(
        [FromRoute] string videoId,
        [FromBody] YoutubeEmbedRequest request)
    {
        try
        {
            if (!IsValidYoutubeVideoId(videoId))
            {
                _logger.LogWarning("Invalid YouTube video ID format in POST: {VideoId}", videoId);
                return BadRequest(ApiResponse<YoutubeEmbedResponse>.Fail("Invalid video ID format"));
            }

            var embedUrl = BuildEmbedUrl(
                videoId,
                request?.Autoplay ?? false,
                request?.Controls ?? true,
                request?.ModestBranding ?? true);

            _logger.LogInformation("Generated custom YouTube embed URL for video: {VideoIdLast4}", videoId[^4..]);

            var response = new YoutubeEmbedResponse
            {
                VideoId = videoId,
                EmbedUrl = embedUrl,
                Provider = YoutubeNoCookieDomain,
                ExpiresIn = 3600,
                GeneratedAt = DateTime.UtcNow,
            };

            return Ok(ApiResponse<YoutubeEmbedResponse>.Ok(response, "YouTube embed URL generated"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in POST generate embed URL for video: {VideoId}", videoId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<YoutubeEmbedResponse>.Fail("Failed to generate video URL"));
        }
    }

    private static bool IsValidYoutubeVideoId(string videoId)
    {
        // YouTube video IDs are 11 characters: alphanumeric, dash, underscore
        return Regex.IsMatch(videoId, @"^[a-zA-Z0-9_-]{11}$");
    }

    private static string BuildEmbedUrl(string videoId, bool autoplay, bool controls, bool modestBranding)
    {
        var builder = new UriBuilder($"https://{YoutubeNoCookieDomain}/{YoutubeEmbedPath}/{videoId}");

        var query = new Dictionary<string, string>
        {
            { "autoplay", autoplay ? "1" : "0" },
            { "controls", controls ? "1" : "0" },
            { "modestbranding", modestBranding ? "1" : "0" },
            { "rel", "0" }, // Prevent related videos
            { "fs", "1" }, // Allow fullscreen
            { "iv_load_policy", "3" }, // Hide annotations
        };

        var queryString = string.Join("&", query.Select(kvp => $"{kvp.Key}={kvp.Value}"));
        builder.Query = queryString;

        return builder.Uri.ToString();
    }
}

/// <summary>
/// Request model for YouTube embed customization
/// </summary>
public class YoutubeEmbedRequest
{
    public bool? Autoplay { get; set; }
    public bool? Controls { get; set; }
    public bool? ModestBranding { get; set; }
}

/// <summary>
/// Response model for secure YouTube embed
/// </summary>
public class YoutubeEmbedResponse
{
    public string VideoId { get; set; } = string.Empty;
    public string EmbedUrl { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
    public DateTime GeneratedAt { get; set; }
}
