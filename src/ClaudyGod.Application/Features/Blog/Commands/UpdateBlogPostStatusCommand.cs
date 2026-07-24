using ClaudyGod.Application.Common.Interfaces;
using ClaudyGod.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClaudyGod.Application.Features.Blog.Commands;

public record UpdateBlogPostStatusCommand(Guid PostId, BlogPostStatus Status) : IRequest;

public class UpdateBlogPostStatusCommandHandler : IRequestHandler<UpdateBlogPostStatusCommand>
{
    private readonly IApplicationDbContext _db;

    public UpdateBlogPostStatusCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(UpdateBlogPostStatusCommand request, CancellationToken ct)
    {
        var post = await _db.BlogPosts.FirstOrDefaultAsync(p => p.Id == request.PostId, ct)
            ?? throw new Domain.Exceptions.NotFoundException("Blog post not found.");

        switch (request.Status)
        {
            case BlogPostStatus.Published:
                post.Publish();
                break;
            case BlogPostStatus.Draft:
                post.Retract();
                break;
            case BlogPostStatus.Archived:
                post.Archive();
                break;
        }

        await _db.SaveChangesAsync(ct);
    }
}
