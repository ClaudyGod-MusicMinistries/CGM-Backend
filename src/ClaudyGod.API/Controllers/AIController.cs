using Asp.Versioning;
using ClaudyGod.Application.Common.Models;
using ClaudyGod.Application.Features.AI.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClaudyGod.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/ai")]
[EnableRateLimiting("ai")]
public class AIController : ControllerBase
{
    private readonly IMediator _mediator;

    public AIController(IMediator mediator) => _mediator = mediator;

    [HttpPost("chat")]
    public async Task<ActionResult<ApiResponse<ChatResponseDto>>> Chat(
        [FromBody] ChatRequestDto dto, CancellationToken ct)
    {
        var reply = await _mediator.Send(new ChatWithAssistantQuery(dto.Message, dto.History), ct);
        return Ok(ApiResponse<ChatResponseDto>.Ok(new ChatResponseDto(reply)));
    }

    [HttpPost("prayer")]
    public async Task<ActionResult<ApiResponse<ChatResponseDto>>> Prayer(
        [FromBody] ChatRequestDto dto, CancellationToken ct)
    {
        var reply = await _mediator.Send(new PrayerChatQuery(dto.Message), ct);
        return Ok(ApiResponse<ChatResponseDto>.Ok(new ChatResponseDto(reply)));
    }

    [HttpPost("booking-help")]
    public async Task<ActionResult<ApiResponse<ChatResponseDto>>> BookingHelp(
        [FromBody] ChatRequestDto dto, CancellationToken ct)
    {
        var reply = await _mediator.Send(new BookingHelpChatQuery(dto.Message), ct);
        return Ok(ApiResponse<ChatResponseDto>.Ok(new ChatResponseDto(reply)));
    }
}

public record ChatRequestDto(string Message, List<ChatMessageDto>? History = null);
public record ChatResponseDto(string Reply);
