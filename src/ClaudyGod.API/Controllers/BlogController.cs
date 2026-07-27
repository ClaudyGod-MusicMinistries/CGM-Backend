using Asp.Versioning;
using ClaudyGod.Application.Common.Models;
using ClaudyGod.Application.Features.Blog.Commands;
using ClaudyGod.Application.Features.Blog.DTOs;
using ClaudyGod.Application.Features.Blog.Queries;
using ClaudyGod.Application.Features.Comments.Commands;
using ClaudyGod.Application.Features.Comments.DTOs;
using ClaudyGod.Application.Features.Comments.Queries;
using ClaudyGod.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClaudyGod.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/blog")]
public class BlogController : ControllerBase
{
    private readonly IMediator _mediator;

    public BlogController(IMediator mediator) => _mediator = mediator;

    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedResult<BlogPostDto>>>> GetAll(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10,
        [FromQuery] BlogPostStatus? status = null,
        [FromQuery] Guid? categoryId = null,
        [FromQuery] bool featuredOnly = false,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetBlogPostsQuery(page, pageSize, status, categoryId, featuredOnly), ct);
        return Ok(ApiResponse<PaginatedResult<BlogPostDto>>.Ok(result));
    }

    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    [HttpGet("{slug}")]
    public async Task<ActionResult<ApiResponse<BlogPostDetailDto>>> GetBySlug(
        string slug, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetBlogPostBySlugQuery(slug), ct);
        return Ok(ApiResponse<BlogPostDetailDto>.Ok(result));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<object>>> Create(
        [FromBody] CreateBlogPostRequest dto, CancellationToken ct)
    {
        var id = await _mediator.Send(new CreateBlogPostCommand(dto), ct);
        return CreatedAtAction(nameof(GetBySlug), new { slug = dto.Slug },
            ApiResponse<object>.Ok(new { id }, "Blog post created."));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse>> Update(
        Guid id, [FromBody] UpdateBlogPostRequest dto, CancellationToken ct)
    {
        await _mediator.Send(new UpdateBlogPostCommand(id, dto), ct);
        return Ok(ApiResponse.Ok("Blog post updated."));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new DeleteBlogPostCommand(id), ct);
        return Ok(ApiResponse.Ok("Blog post deleted."));
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<ApiResponse>> UpdateStatus(
        Guid id, [FromBody] UpdateBlogPostStatusRequest dto, CancellationToken ct)
    {
        if (!Enum.TryParse<BlogPostStatus>(dto.Status, ignoreCase: true, out var parsed))
            throw new ClaudyGod.Domain.Exceptions.ValidationException(
                new Dictionary<string, string[]>
                {
                    ["status"] = [$"Status must be one of: {string.Join(", ", Enum.GetNames<BlogPostStatus>())}."]
                });

        await _mediator.Send(new UpdateBlogPostStatusCommand(id, parsed), ct);
        return Ok(ApiResponse.Ok("Blog post status updated."));
    }

    // ── Categories & tags ──────────────────────────────────────────────────
    // Literal segments ("categories", "tags") are matched ahead of the
    // parameterized {slug} route above — standard ASP.NET routing precedence,
    // no ambiguity with GetBySlug.

    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    [HttpGet("categories")]
    public async Task<ActionResult<ApiResponse<List<BlogCategoryDto>>>> GetCategories(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetBlogCategoriesQuery(), ct);
        return Ok(ApiResponse<List<BlogCategoryDto>>.Ok(result));
    }

    [HttpPost("categories")]
    public async Task<ActionResult<ApiResponse<object>>> CreateCategory(
        [FromBody] CreateBlogCategoryRequest dto, CancellationToken ct)
    {
        var id = await _mediator.Send(new CreateBlogCategoryCommand(dto), ct);
        return CreatedAtAction(nameof(GetCategories), ApiResponse<object>.Ok(new { id }, "Category created."));
    }

    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    [HttpGet("tags")]
    public async Task<ActionResult<ApiResponse<List<BlogTagDto>>>> GetTags(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetBlogTagsQuery(), ct);
        return Ok(ApiResponse<List<BlogTagDto>>.Ok(result));
    }

    [HttpPost("tags")]
    public async Task<ActionResult<ApiResponse<object>>> CreateTag(
        [FromBody] CreateBlogTagRequest dto, CancellationToken ct)
    {
        var id = await _mediator.Send(new CreateBlogTagCommand(dto), ct);
        return CreatedAtAction(nameof(GetTags), ApiResponse<object>.Ok(new { id }, "Tag created."));
    }

    // ── Comments ──────────────────────────────────────────────────────────────
    // Post-scoped, public, anonymous (no login exists on the website) — the
    // admin moderation actions (list all/approve/reject/delete) live on the
    // separate top-level CommentController since they aren't post-scoped.

    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    [HttpGet("{id:guid}/comments")]
    public async Task<ActionResult<ApiResponse<List<CommentDto>>>> GetComments(
        Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetCommentsForPostQuery(id), ct);
        return Ok(ApiResponse<List<CommentDto>>.Ok(result));
    }

    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    [EnableRateLimiting("comments")]
    [HttpPost("{id:guid}/comments")]
    public async Task<ActionResult<ApiResponse<object>>> CreateComment(
        Guid id, [FromBody] CreateCommentRequest dto, CancellationToken ct)
    {
        var commentId = await _mediator.Send(new CreateCommentCommand(id, dto), ct);
        // A tripped honeypot returns the same success shape with no real id —
        // the caller (bot or human) can't tell the difference.
        return Ok(ApiResponse<object>.Ok(new { id = commentId },
            "Comment submitted — it will appear once approved."));
    }

    // ── Reactions ─────────────────────────────────────────────────────────────
    // Post-level here (post-scoped, same convention as comments above);
    // comment-level reactions sit under the literal "comments" segment below
    // — a different route shape from {id:guid}/comments above, so there's no
    // precedence conflict, same as the categories/tags pattern.

    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    [HttpGet("{id:guid}/reactions")]
    public async Task<ActionResult<ApiResponse<ReactionSummaryDto>>> GetPostReactions(
        Guid id, [FromQuery] string? visitorToken, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetReactionSummaryQuery(id, null, visitorToken), ct);
        return Ok(ApiResponse<ReactionSummaryDto>.Ok(result));
    }

    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    [EnableRateLimiting("comments")]
    [HttpPost("{id:guid}/reactions")]
    public async Task<ActionResult<ApiResponse<ReactionSummaryDto>>> SetPostReaction(
        Guid id, [FromBody] SetReactionRequest dto, CancellationToken ct)
    {
        var result = await _mediator.Send(new SetReactionCommand(id, null, dto.VisitorToken, dto.Emoji), ct);
        return Ok(ApiResponse<ReactionSummaryDto>.Ok(result));
    }

    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    [EnableRateLimiting("comments")]
    [HttpDelete("{id:guid}/reactions")]
    public async Task<ActionResult<ApiResponse<ReactionSummaryDto>>> RemovePostReaction(
        Guid id, [FromQuery] string visitorToken, CancellationToken ct)
    {
        var result = await _mediator.Send(new RemoveReactionCommand(id, null, visitorToken), ct);
        return Ok(ApiResponse<ReactionSummaryDto>.Ok(result));
    }

    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    [HttpGet("comments/{commentId:guid}/reactions")]
    public async Task<ActionResult<ApiResponse<ReactionSummaryDto>>> GetCommentReactions(
        Guid commentId, [FromQuery] string? visitorToken, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetReactionSummaryQuery(null, commentId, visitorToken), ct);
        return Ok(ApiResponse<ReactionSummaryDto>.Ok(result));
    }

    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    [EnableRateLimiting("comments")]
    [HttpPost("comments/{commentId:guid}/reactions")]
    public async Task<ActionResult<ApiResponse<ReactionSummaryDto>>> SetCommentReaction(
        Guid commentId, [FromBody] SetReactionRequest dto, CancellationToken ct)
    {
        var result = await _mediator.Send(new SetReactionCommand(null, commentId, dto.VisitorToken, dto.Emoji), ct);
        return Ok(ApiResponse<ReactionSummaryDto>.Ok(result));
    }

    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    [EnableRateLimiting("comments")]
    [HttpDelete("comments/{commentId:guid}/reactions")]
    public async Task<ActionResult<ApiResponse<ReactionSummaryDto>>> RemoveCommentReaction(
        Guid commentId, [FromQuery] string visitorToken, CancellationToken ct)
    {
        var result = await _mediator.Send(new RemoveReactionCommand(null, commentId, visitorToken), ct);
        return Ok(ApiResponse<ReactionSummaryDto>.Ok(result));
    }
}

public record UpdateBlogPostStatusRequest(string Status);
