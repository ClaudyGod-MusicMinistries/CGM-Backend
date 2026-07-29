using Asp.Versioning;
using ClaudyGod.API.Attributes;
using ClaudyGod.Application.Common.Models;
using ClaudyGod.Application.Features.FAQs.Commands;
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
    [PublicEndpoint]
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
    [PublicEndpoint]
    [HttpGet("categories/{category}")]
    public async Task<ActionResult<ApiResponse<List<FAQDto>>>> GetByCategory(
        string category,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetFAQsQuery(category), ct);
        return Ok(ApiResponse<List<FAQDto>>.Ok(result));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<object>>> Create(
        [FromBody] CreateFAQRequest dto, CancellationToken ct)
    {
        var id = await _mediator.Send(new CreateFAQCommand(dto), ct);
        return CreatedAtAction(nameof(GetAll), ApiResponse<object>.Ok(new { id }, "FAQ created."));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse>> Update(
        Guid id, [FromBody] CreateFAQRequest dto, CancellationToken ct)
    {
        await _mediator.Send(new UpdateFAQCommand(id, dto), ct);
        return Ok(ApiResponse.Ok("FAQ updated."));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new DeleteFAQCommand(id), ct);
        return Ok(ApiResponse.Ok("FAQ deleted."));
    }
}
