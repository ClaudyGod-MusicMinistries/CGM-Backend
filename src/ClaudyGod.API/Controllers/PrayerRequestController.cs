using ClaudyGod.Application.Common.Models;
using ClaudyGod.Application.Features.PrayerRequests.Commands;
using ClaudyGod.Application.Features.PrayerRequests.Queries;
using ClaudyGod.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClaudyGod.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/prayer-requests")]
public class PrayerRequestController : ControllerBase
{
    private readonly IMediator _mediator;

    public PrayerRequestController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    public async Task<ActionResult<ApiResponse<object>>> Submit(
        [FromBody] SubmitPrayerRequestDto dto, CancellationToken ct)
    {
        var id = await _mediator.Send(new SubmitPrayerRequestCommand(
            dto.Name, dto.Email, dto.Subject, dto.RequestText, dto.IsConfidential), ct);
        return Ok(ApiResponse<object>.Ok(new { id }, "Prayer request submitted. We will intercede for you."));
    }

    [Authorize(Roles = "Admin,SuperAdmin")]
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedResult<PrayerRequestDto>>>> GetAll(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] PrayerRequestStatus? status = null,
        [FromQuery] bool includeConfidential = true,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetPrayerRequestsQuery(page, pageSize, status, includeConfidential), ct);
        return Ok(ApiResponse<PaginatedResult<PrayerRequestDto>>.Ok(result));
    }
}

public record SubmitPrayerRequestDto(
    string Name, string Email, string Subject, string RequestText,
    bool IsConfidential = false);
