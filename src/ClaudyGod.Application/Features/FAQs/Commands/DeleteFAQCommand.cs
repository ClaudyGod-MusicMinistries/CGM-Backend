using ClaudyGod.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClaudyGod.Application.Features.FAQs.Commands;

public record DeleteFAQCommand(Guid FAQId) : IRequest;

public class DeleteFAQCommandHandler : IRequestHandler<DeleteFAQCommand>
{
    private readonly IApplicationDbContext _db;

    public DeleteFAQCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(DeleteFAQCommand request, CancellationToken ct)
    {
        var faq = await _db.FAQs.FirstOrDefaultAsync(f => f.Id == request.FAQId, ct)
            ?? throw new Domain.Exceptions.NotFoundException("FAQ not found.");

        faq.IsDeleted = true;
        faq.DeletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }
}
