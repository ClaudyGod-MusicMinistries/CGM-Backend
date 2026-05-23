using ClaudyGod.Application.Common.Interfaces;
using ClaudyGod.Application.Features.Volunteers.DTOs;
using ClaudyGod.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ClaudyGod.Application.Features.Volunteers.Commands;

public record RegisterVolunteerCommand(RegisterVolunteerRequest Request) : IRequest<Guid>;

public class RegisterVolunteerCommandValidator : AbstractValidator<RegisterVolunteerCommand>
{
    public RegisterVolunteerCommandValidator()
    {
        RuleFor(x => x.Request.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(100);
        RuleFor(x => x.Request.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(100);
        RuleFor(x => x.Request.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.");
        RuleFor(x => x.Request.Reason)
            .NotEmpty().WithMessage("Please tell us why you want to volunteer.")
            .MinimumLength(20).WithMessage("Please share a bit more about your passion (minimum 20 characters).")
            .MaximumLength(2000);
    }
}

public class RegisterVolunteerCommandHandler : IRequestHandler<RegisterVolunteerCommand, Guid>
{
    private readonly IApplicationDbContext _db;
    private readonly IEmailService _email;
    private readonly ILogger<RegisterVolunteerCommandHandler> _logger;

    public RegisterVolunteerCommandHandler(
        IApplicationDbContext db,
        IEmailService email,
        ILogger<RegisterVolunteerCommandHandler> logger)
    {
        _db = db;
        _email = email;
        _logger = logger;
    }

    public async Task<Guid> Handle(RegisterVolunteerCommand request, CancellationToken ct)
    {
        var r = request.Request;
        var volunteer = Volunteer.Create(r.FirstName, r.LastName, r.Email, r.Role, r.Reason);
        _db.Volunteers.Add(volunteer);
        await _db.SaveChangesAsync(ct);

        await _email.TrySendFromTemplateAsync(r.Email, "volunteer-confirmation", new Dictionary<string, string>
        {
            ["subject"] = "Thank You for Volunteering – ClaudyGod Ministry",
            ["name"] = $"{r.FirstName} {r.LastName}",
            ["role"] = r.Role.ToString()
        }, _logger, ct);

        return volunteer.Id;
    }
}
