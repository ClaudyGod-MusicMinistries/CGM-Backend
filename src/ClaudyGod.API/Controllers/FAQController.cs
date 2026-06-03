using Asp.Versioning;
using ClaudyGod.Application.Common.Models;
using ClaudyGod.Application.Features.FAQs.DTOs;
using ClaudyGod.Application.Features.FAQs.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ClaudyGod.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/faqs")]
public class FAQController : ControllerBase
{
    private readonly IMediator _mediator;

    public FAQController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Get all FAQs, optionally filtered by category
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<FAQDto>>>> GetAll(
        [FromQuery] string? category = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetFAQsQuery(category), ct);
        return Ok(ApiResponse<List<FAQDto>>.Ok(result));
    }

    /// <summary>
    /// Get FAQs by category
    /// </summary>
    [HttpGet("categories/{category}")]
    public async Task<ActionResult<ApiResponse<List<FAQDto>>>> GetByCategory(
        string category,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetFAQsQuery(category), ct);
        return Ok(ApiResponse<List<FAQDto>>.Ok(result));
    }
}
