using ClaudyGod.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClaudyGod.Application.Features.PrayerRequests.Commands;

public record DeletePrayerRequestCommand(Guid PrayerRequestId) : IRequest;

public class DeletePrayerRequestCommandHandler : IRequestHandler<DeletePrayerRequestCommand>
{
    private readonly IApplicationDbContext _db;

    public DeletePrayerRequestCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(DeletePrayerRequestCommand request, CancellationToken ct)
    {
        var prayerRequest = await _db.PrayerRequests.FirstOrDefaultAsync(p => p.Id == request.PrayerRequestId, ct)
            ?? throw new Domain.Exceptions.NotFoundException("Prayer request not found.");

        prayerRequest.IsDeleted = true;
        prayerRequest.DeletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }
}
