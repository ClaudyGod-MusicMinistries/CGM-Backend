using ClaudyGod.Application.Common.Interfaces;
using ClaudyGod.Application.Features.Volunteers.DTOs;
using ClaudyGod.Domain.Entities;
using ClaudyGod.Domain.Enums;
using FluentValidation;
using MediatR;

namespace ClaudyGod.Application.Features.Volunteers.Commands;

public record RegisterVolunteerCommand(RegisterVolunteerRequest Request) : IRequest<Guid>;

public class RegisterVolunteerCommandValidator : AbstractValidator<RegisterVolunteerCommand>
{
    public RegisterVolunteerCommandValidator()
    {
        RuleFor(x => x.Request.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Request.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Request.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Request.Reason).NotEmpty().MinimumLength(20).MaximumLength(2000);
    }
}

public class RegisterVolunteerCommandHandler : IRequestHandler<RegisterVolunteerCommand, Guid>
{
    private readonly IApplicationDbContext _db;
    private readonly IEmailService _email;

    public RegisterVolunteerCommandHandler(IApplicationDbContext db, IEmailService email)
    {
        _db = db;
        _email = email;
    }

    public async Task<Guid> Handle(RegisterVolunteerCommand request, CancellationToken ct)
    {
        var r = request.Request;
        var volunteer = Volunteer.Create(r.FirstName, r.LastName, r.Email, r.Role, r.Reason);
        _db.Volunteers.Add(volunteer);
        await _db.SaveChangesAsync(ct);

        await _email.SendFromTemplateAsync(r.Email, "volunteer-confirmation", new Dictionary<string, string>
        {
            ["subject"] = "Thank You for Volunteering – ClaudyGod Ministry",
            ["name"] = $"{r.FirstName} {r.LastName}",
            ["role"] = r.Role.ToString()
        }, ct);

        return volunteer.Id;
    }
}
