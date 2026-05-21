using ClaudyGod.Application.Common.Interfaces;
using ClaudyGod.Application.Features.Blog.DTOs;
using ClaudyGod.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClaudyGod.Application.Features.Blog.Commands;

public record CreateBlogPostCommand(CreateBlogPostRequest Request) : IRequest<Guid>;

public class CreateBlogPostCommandValidator : AbstractValidator<CreateBlogPostCommand>
{
    public CreateBlogPostCommandValidator()
    {
        RuleFor(x => x.Request.Title).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Request.Slug).NotEmpty().MaximumLength(500)
            .Matches("^[a-z0-9-]+$").WithMessage("Slug may only contain lowercase letters, numbers, and hyphens.");
        RuleFor(x => x.Request.Content).NotEmpty();
    }
}

public class CreateBlogPostCommandHandler : IRequestHandler<CreateBlogPostCommand, Guid>
{
    private readonly IApplicationDbContext _db;

    public CreateBlogPostCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Guid> Handle(CreateBlogPostCommand request, CancellationToken ct)
    {
        var r = request.Request;

        var slugExists = await _db.BlogPosts.AnyAsync(p => p.Slug == r.Slug, ct);
        if (slugExists) throw new Domain.Exceptions.DuplicateResourceException($"Slug '{r.Slug}' is already in use.");

        var post = BlogPost.Create(r.Title, r.Slug, r.Content, r.Excerpt, r.AuthorName, r.CategoryId);

        if (r.Publish) post.Publish();

        if (r.TagIds?.Count > 0)
        {
            foreach (var tagId in r.TagIds)
                post.PostTags.Add(new BlogPostTag { BlogPostId = post.Id, BlogTagId = tagId });
        }

        _db.BlogPosts.Add(post);
        await _db.SaveChangesAsync(ct);

        return post.Id;
    }
}
