using ClaudyGod.Application.Common.Interfaces;
using ClaudyGod.Domain.Entities;
using FluentValidation;
using MediatR;

namespace ClaudyGod.Application.Features.Contacts.Commands;

public record SubmitContactCommand(string Name, string Email, string Message) : IRequest<Guid>;

public class SubmitContactCommandValidator : AbstractValidator<SubmitContactCommand>
{
    public SubmitContactCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Message).NotEmpty().MinimumLength(10).MaximumLength(5000);
    }
}

public class SubmitContactCommandHandler : IRequestHandler<SubmitContactCommand, Guid>
{
    private readonly IApplicationDbContext _db;
    private readonly IEmailService _email;

    public SubmitContactCommandHandler(IApplicationDbContext db, IEmailService email)
    {
        _db = db;
        _email = email;
    }

    public async Task<Guid> Handle(SubmitContactCommand request, CancellationToken ct)
    {
        var message = ContactMessage.Create(request.Name, request.Email, request.Message);
        _db.ContactMessages.Add(message);
        await _db.SaveChangesAsync(ct);

        await _email.SendFromTemplateAsync(request.Email, "contact-confirmation", new Dictionary<string, string>
        {
            ["subject"] = "We received your message",
            ["name"] = request.Name,
            ["message"] = request.Message
        }, ct);

        return message.Id;
    }
}
