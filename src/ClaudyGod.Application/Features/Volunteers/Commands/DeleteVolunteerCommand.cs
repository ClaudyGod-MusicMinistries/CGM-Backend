using ClaudyGod.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClaudyGod.Application.Features.Volunteers.Commands;

public record DeleteVolunteerCommand(Guid VolunteerId) : IRequest;

public class DeleteVolunteerCommandHandler : IRequestHandler<DeleteVolunteerCommand>
{
    private readonly IApplicationDbContext _db;

    public DeleteVolunteerCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(DeleteVolunteerCommand request, CancellationToken ct)
    {
        var volunteer = await _db.Volunteers.FirstOrDefaultAsync(v => v.Id == request.VolunteerId, ct)
            ?? throw new Domain.Exceptions.NotFoundException("Volunteer application not found.");

        volunteer.IsDeleted = true;
        volunteer.DeletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }
}
