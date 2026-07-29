using ClaudyGod.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClaudyGod.Application.Features.Media.Commands;

public record DeleteMediaCommand(Guid MediaId) : IRequest;

public class DeleteMediaCommandHandler : IRequestHandler<DeleteMediaCommand>
{
    private readonly IApplicationDbContext _db;

    public DeleteMediaCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(DeleteMediaCommand request, CancellationToken ct)
    {
        var media = await _db.MediaItems.FirstOrDefaultAsync(m => m.Id == request.MediaId, ct)
            ?? throw new Domain.Exceptions.NotFoundException("Media item not found.");

        media.IsDeleted = true;
        media.DeletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }
}
