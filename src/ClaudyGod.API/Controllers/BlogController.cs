using ClaudyGod.Application.Common.Models;
using ClaudyGod.Application.Features.Blog.Commands;
using ClaudyGod.Application.Features.Blog.DTOs;
using ClaudyGod.Application.Features.Blog.Queries;
using ClaudyGod.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClaudyGod.API.Controllers;

[ApiController]
[Route("api/blog")]
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

    [Authorize(Roles = "Admin,SuperAdmin")]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<object>>> Create(
        [FromBody] CreateBlogPostRequest dto, CancellationToken ct)
    {
        var id = await _mediator.Send(new CreateBlogPostCommand(dto), ct);
        return CreatedAtAction(nameof(GetBySlug), new { slug = dto.Slug },
            ApiResponse<object>.Ok(new { id }, "Blog post created."));
    }

    [Authorize(Roles = "Admin,SuperAdmin")]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse>> Update(
        Guid id, [FromBody] UpdateBlogPostRequest dto, CancellationToken ct)
    {
        await _mediator.Send(new UpdateBlogPostCommand(id, dto), ct);
        return Ok(ApiResponse.Ok("Blog post updated."));
    }

    [Authorize(Roles = "Admin,SuperAdmin")]
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new DeleteBlogPostCommand(id), ct);
        return Ok(ApiResponse.Ok("Blog post deleted."));
    }
}
