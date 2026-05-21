using ClaudyGod.Application.Common.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClaudyGod.Application.Features.Subscribers.Commands;

public record UnsubscribeCommand(string Email, string Token) : IRequest;

public class UnsubscribeCommandValidator : AbstractValidator<UnsubscribeCommand>
{
    public UnsubscribeCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Token).NotEmpty();
    }
}

public class UnsubscribeCommandHandler : IRequestHandler<UnsubscribeCommand>
{
    private readonly IApplicationDbContext _db;

    public UnsubscribeCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(UnsubscribeCommand request, CancellationToken ct)
    {
        var subscriber = await _db.Subscribers
            .FirstOrDefaultAsync(s => s.Email == request.Email.ToLowerInvariant()
                && s.UnsubscribeToken == request.Token, ct)
            ?? throw new Domain.Exceptions.NotFoundException("Subscriber not found.");

        subscriber.Unsubscribe();
        await _db.SaveChangesAsync(ct);
    }
}
