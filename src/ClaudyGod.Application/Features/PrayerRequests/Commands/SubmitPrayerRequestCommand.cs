using ClaudyGod.Application.Common.Interfaces;
using ClaudyGod.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ClaudyGod.Application.Features.PrayerRequests.Commands;

public record SubmitPrayerRequestCommand(
    string Name, string Email, string Subject, string RequestText,
    bool IsConfidential = false) : IRequest<Guid>;

public class SubmitPrayerRequestCommandValidator : AbstractValidator<SubmitPrayerRequestCommand>
{
    public SubmitPrayerRequestCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(100);
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.");
        RuleFor(x => x.Subject)
            .NotEmpty().WithMessage("Subject is required.")
            .MaximumLength(300);
        RuleFor(x => x.RequestText)
            .NotEmpty().WithMessage("Prayer request details are required.")
            .MaximumLength(5000);
    }
}

public class SubmitPrayerRequestCommandHandler : IRequestHandler<SubmitPrayerRequestCommand, Guid>
{
    private readonly IApplicationDbContext _db;
    private readonly IEmailService _email;
    private readonly ILogger<SubmitPrayerRequestCommandHandler> _logger;

    public SubmitPrayerRequestCommandHandler(
        IApplicationDbContext db,
        IEmailService email,
        ILogger<SubmitPrayerRequestCommandHandler> logger)
    {
        _db = db;
        _email = email;
        _logger = logger;
    }

    public async Task<Guid> Handle(SubmitPrayerRequestCommand request, CancellationToken ct)
    {
        var prayerRequest = PrayerRequest.Create(request.Name, request.Email,
            request.Subject, request.RequestText, request.IsConfidential);

        _db.PrayerRequests.Add(prayerRequest);
        await _db.SaveChangesAsync(ct);

        await _email.TrySendFromTemplateAsync(request.Email, "prayer-received", new Dictionary<string, string>
        {
            ["subject"] = "Your Prayer Request Has Been Received – ClaudyGod Ministry",
            ["name"] = request.Name,
            ["prayerSubject"] = request.Subject
        }, _logger, ct);

        return prayerRequest.Id;
    }
}
