using Asp.Versioning;
using ClaudyGod.Application.Common.Interfaces;
using ClaudyGod.Application.Common.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClaudyGod.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/ai")]
[EnableRateLimiting("ai")]
public class AIController : ControllerBase
{
    private readonly IAIService _ai;
    private readonly ILogger<AIController> _logger;

    public AIController(IAIService ai, ILogger<AIController> logger)
    {
        _ai     = ai;
        _logger = logger;
    }

    [HttpPost("chat")]
    public async Task<ActionResult<ApiResponse<ChatResponseDto>>> Chat(
        [FromBody] ChatRequestDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.Message) || dto.Message.Length > 1000)
            return BadRequest(ApiResponse.Fail("Message must be between 1 and 1000 characters."));

        var history = dto.History?
            .Take(10) // limit context window to last 10 turns
            .Select(m => new ChatMessage(m.Role, m.Content))
            .ToList();

        var reply = await _ai.ChatAsync(dto.Message, history, AIPersona.MinistryAssistant, ct);
        return Ok(ApiResponse<ChatResponseDto>.Ok(new ChatResponseDto(reply)));
    }

    [HttpPost("prayer")]
    public async Task<ActionResult<ApiResponse<ChatResponseDto>>> Prayer(
        [FromBody] ChatRequestDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.Message) || dto.Message.Length > 2000)
            return BadRequest(ApiResponse.Fail("Prayer request must be between 1 and 2000 characters."));

        var reply = await _ai.ChatAsync(dto.Message, null, AIPersona.PrayerCompanion, ct);
        return Ok(ApiResponse<ChatResponseDto>.Ok(new ChatResponseDto(reply)));
    }

    [HttpPost("booking-help")]
    public async Task<ActionResult<ApiResponse<ChatResponseDto>>> BookingHelp(
        [FromBody] ChatRequestDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.Message) || dto.Message.Length > 1000)
            return BadRequest(ApiResponse.Fail("Message must be between 1 and 1000 characters."));

        var reply = await _ai.ChatAsync(dto.Message, null, AIPersona.BookingHelper, ct);
        return Ok(ApiResponse<ChatResponseDto>.Ok(new ChatResponseDto(reply)));
    }
}

public record ChatRequestDto(string Message, List<ChatMessageDto>? History = null);
public record ChatMessageDto(string Role, string Content);
public record ChatResponseDto(string Reply);
