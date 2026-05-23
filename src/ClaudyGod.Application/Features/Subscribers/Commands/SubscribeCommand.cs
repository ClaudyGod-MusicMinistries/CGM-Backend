using ClaudyGod.Application.Common.Interfaces;
using ClaudyGod.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ClaudyGod.Application.Features.Subscribers.Commands;

public record SubscribeCommand(string Name, string Email) : IRequest<Guid>;

public class SubscribeCommandValidator : AbstractValidator<SubscribeCommand>
{
    public SubscribeCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(100);
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.")
            .MaximumLength(256);
    }
}

public class SubscribeCommandHandler : IRequestHandler<SubscribeCommand, Guid>
{
    private readonly IApplicationDbContext _db;
    private readonly IEmailService _email;
    private readonly ILogger<SubscribeCommandHandler> _logger;

    public SubscribeCommandHandler(
        IApplicationDbContext db,
        IEmailService email,
        ILogger<SubscribeCommandHandler> logger)
    {
        _db = db;
        _email = email;
        _logger = logger;
    }

    public async Task<Guid> Handle(SubscribeCommand request, CancellationToken ct)
    {
        var normalizedEmail = request.Email.ToLowerInvariant().Trim();

        var existing = await _db.Subscribers
            .FirstOrDefaultAsync(s => s.Email == normalizedEmail, ct);

        if (existing is not null)
        {
            if (existing.IsActive)
                throw new Domain.Exceptions.DuplicateResourceException("This email is already subscribed.");

            existing.Resubscribe();
            await _db.SaveChangesAsync(ct);
            await SendWelcomeEmailAsync(existing, ct);
            return existing.Id;
        }

        var subscriber = Subscriber.Create(request.Name, normalizedEmail);
        _db.Subscribers.Add(subscriber);
        await _db.SaveChangesAsync(ct);
        await SendWelcomeEmailAsync(subscriber, ct);

        return subscriber.Id;
    }

    private Task SendWelcomeEmailAsync(Subscriber subscriber, CancellationToken ct) =>
        _email.TrySendFromTemplateAsync(subscriber.Email, "welcome", new Dictionary<string, string>
        {
            ["subject"] = "Welcome to the ClaudyGod Community!",
            ["name"] = subscriber.Name,
            ["unsubscribeToken"] = subscriber.UnsubscribeToken,
            ["unsubscribeEmail"] = subscriber.Email
        }, _logger, ct);
}
