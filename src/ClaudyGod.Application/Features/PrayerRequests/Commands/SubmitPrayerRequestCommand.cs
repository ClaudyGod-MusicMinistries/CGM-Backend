using ClaudyGod.Application.Common.Interfaces;
using ClaudyGod.Domain.Entities;
using FluentValidation;
using MediatR;

namespace ClaudyGod.Application.Features.PrayerRequests.Commands;

public record SubmitPrayerRequestCommand(
    string Name, string Email, string Subject, string RequestText,
    bool IsConfidential = false) : IRequest<Guid>;

public class SubmitPrayerRequestCommandValidator : AbstractValidator<SubmitPrayerRequestCommand>
{
    public SubmitPrayerRequestCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Subject).NotEmpty().MaximumLength(300);
        RuleFor(x => x.RequestText).NotEmpty().MaximumLength(5000);
    }
}

public class SubmitPrayerRequestCommandHandler : IRequestHandler<SubmitPrayerRequestCommand, Guid>
{
    private readonly IApplicationDbContext _db;
    private readonly IEmailService _email;

    public SubmitPrayerRequestCommandHandler(IApplicationDbContext db, IEmailService email)
    {
        _db = db;
        _email = email;
    }

    public async Task<Guid> Handle(SubmitPrayerRequestCommand request, CancellationToken ct)
    {
        var prayerRequest = PrayerRequest.Create(request.Name, request.Email,
            request.Subject, request.RequestText, request.IsConfidential);

        _db.PrayerRequests.Add(prayerRequest);
        await _db.SaveChangesAsync(ct);

        await _email.SendFromTemplateAsync(request.Email, "prayer-received", new Dictionary<string, string>
        {
            ["subject"] = "Your Prayer Request Has Been Received",
            ["name"] = request.Name,
            ["prayerSubject"] = request.Subject
        }, ct);

        return prayerRequest.Id;
    }
}
