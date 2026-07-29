using Asp.Versioning;
using ClaudyGod.Application.Common.Models;
using ClaudyGod.Application.Features.Comments.Commands;
using ClaudyGod.Application.Features.Comments.DTOs;
using ClaudyGod.Application.Features.Comments.Queries;
using ClaudyGod.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ClaudyGod.API.Controllers;

// Admin moderation surface for comments left on Blog posts — not post-scoped
// (lists across every post), so this stays a separate top-level controller
// from BlogController's own post-scoped public comment/like actions.
// Protected by the secure admin fallback policy, like every other admin-write endpoint.
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/comments")]
public class CommentController : ControllerBase
{
    private readonly IMediator _mediator;

    public CommentController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedResult<AdminCommentDto>>>> GetAll(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] CommentStatus? status = null, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetAdminCommentsQuery(page, pageSize, status), ct);
        return Ok(ApiResponse<PaginatedResult<AdminCommentDto>>.Ok(result));
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<ApiResponse>> UpdateStatus(
        Guid id, [FromBody] UpdateCommentStatusRequest dto, CancellationToken ct)
    {
        if (!Enum.TryParse<CommentStatus>(dto.Status, ignoreCase: true, out var parsed))
            throw new ClaudyGod.Domain.Exceptions.ValidationException(
                new Dictionary<string, string[]>
                {
                    ["status"] = [$"Status must be one of: {string.Join(", ", Enum.GetNames<CommentStatus>())}."]
                });

        await _mediator.Send(new UpdateCommentStatusCommand(id, parsed), ct);
        return Ok(ApiResponse.Ok("Comment status updated."));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new DeleteCommentCommand(id), ct);
        return Ok(ApiResponse.Ok("Comment deleted."));
    }
}
