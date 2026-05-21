using ClaudyGod.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClaudyGod.Application.Features.Blog.Commands;

public record DeleteBlogPostCommand(Guid PostId) : IRequest;

public class DeleteBlogPostCommandHandler : IRequestHandler<DeleteBlogPostCommand>
{
    private readonly IApplicationDbContext _db;

    public DeleteBlogPostCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(DeleteBlogPostCommand request, CancellationToken ct)
    {
        var post = await _db.BlogPosts.FirstOrDefaultAsync(p => p.Id == request.PostId, ct)
            ?? throw new Domain.Exceptions.NotFoundException("Blog post not found.");

        post.IsDeleted = true;
        post.DeletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }
}
