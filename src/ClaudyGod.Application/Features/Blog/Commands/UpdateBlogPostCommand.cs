using ClaudyGod.Application.Common.Interfaces;
using ClaudyGod.Application.Features.Blog.DTOs;
using ClaudyGod.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClaudyGod.Application.Features.Blog.Commands;

public record UpdateBlogPostCommand(Guid PostId, UpdateBlogPostRequest Request) : IRequest;

public class UpdateBlogPostCommandValidator : AbstractValidator<UpdateBlogPostCommand>
{
    public UpdateBlogPostCommandValidator()
    {
        RuleFor(x => x.Request.Title).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Request.Slug).NotEmpty().MaximumLength(500)
            .Matches("^[a-z0-9-]+$").WithMessage("Slug may only contain lowercase letters, numbers, and hyphens.");
        RuleFor(x => x.Request.Content).NotEmpty();
    }
}

public class UpdateBlogPostCommandHandler : IRequestHandler<UpdateBlogPostCommand>
{
    private readonly IApplicationDbContext _db;

    public UpdateBlogPostCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(UpdateBlogPostCommand request, CancellationToken ct)
    {
        var post = await _db.BlogPosts
            .Include(p => p.PostTags)
            .FirstOrDefaultAsync(p => p.Id == request.PostId, ct)
            ?? throw new Domain.Exceptions.NotFoundException("Blog post not found.");

        var r = request.Request;
        post.Update(r.Title, r.Slug, r.Content, r.Excerpt, r.AuthorName, r.CategoryId);

        post.PostTags.Clear();
        if (r.TagIds?.Count > 0)
        {
            foreach (var tagId in r.TagIds)
                post.PostTags.Add(new BlogPostTag { BlogPostId = post.Id, BlogTagId = tagId });
        }

        await _db.SaveChangesAsync(ct);
    }
}
