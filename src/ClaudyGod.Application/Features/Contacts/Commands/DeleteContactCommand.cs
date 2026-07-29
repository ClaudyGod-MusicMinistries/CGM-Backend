using ClaudyGod.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClaudyGod.Application.Features.Contacts.Commands;

public record DeleteContactCommand(Guid ContactId) : IRequest;

public class DeleteContactCommandHandler : IRequestHandler<DeleteContactCommand>
{
    private readonly IApplicationDbContext _db;

    public DeleteContactCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(DeleteContactCommand request, CancellationToken ct)
    {
        var contact = await _db.ContactMessages.FirstOrDefaultAsync(c => c.Id == request.ContactId, ct)
            ?? throw new Domain.Exceptions.NotFoundException("Contact message not found.");

        contact.IsDeleted = true;
        contact.DeletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }
}
