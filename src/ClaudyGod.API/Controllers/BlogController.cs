using Asp.Versioning;
using ClaudyGod.Application.Common.Models;
using ClaudyGod.Application.Features.Blog.Commands;
using ClaudyGod.Application.Features.Blog.DTOs;
using ClaudyGod.Application.Features.Blog.Queries;
using ClaudyGod.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ClaudyGod.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/blog")]
public class BlogController : ControllerBase
{
    private readonly IMediator _mediator;

    public BlogController(IMediator mediator) => _mediator = mediator;

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
            return BadRequest(ApiResponse.Fail(
                $"Invalid blog post status '{dto.Status}'. Valid values: {string.Join(", ", Enum.GetNames<BlogPostStatus>())}"));

        await _mediator.Send(new UpdateBlogPostStatusCommand(id, parsed), ct);
        return Ok(ApiResponse.Ok("Blog post status updated."));
    }

    // ── Categories & tags ──────────────────────────────────────────────────
    // Literal segments ("categories", "tags") are matched ahead of the
    // parameterized {slug} route above — standard ASP.NET routing precedence,
    // no ambiguity with GetBySlug.

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
}

public record UpdateBlogPostStatusRequest(string Status);
