using ClaudyGod.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClaudyGod.Application.Features.Albums.Commands;

public record DeleteAlbumCommand(Guid AlbumId) : IRequest;

public class DeleteAlbumCommandHandler : IRequestHandler<DeleteAlbumCommand>
{
    private readonly IApplicationDbContext _db;

    public DeleteAlbumCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(DeleteAlbumCommand request, CancellationToken ct)
    {
        var album = await _db.Albums.FirstOrDefaultAsync(a => a.Id == request.AlbumId, ct)
            ?? throw new Domain.Exceptions.NotFoundException("Album not found.");

        album.IsDeleted = true;
        album.DeletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }
}
