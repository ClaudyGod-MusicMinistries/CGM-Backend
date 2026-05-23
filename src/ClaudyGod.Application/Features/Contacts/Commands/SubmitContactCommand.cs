using ClaudyGod.Application.Common.Interfaces;
using ClaudyGod.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ClaudyGod.Application.Features.Contacts.Commands;

public record SubmitContactCommand(string Name, string Email, string Message) : IRequest<Guid>;

public class SubmitContactCommandValidator : AbstractValidator<SubmitContactCommand>
{
    public SubmitContactCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.").MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().WithMessage("Email is required.").EmailAddress().WithMessage("A valid email address is required.").MaximumLength(256);
        RuleFor(x => x.Message).NotEmpty().WithMessage("Message is required.").MinimumLength(10).WithMessage("Message must be at least 10 characters.").MaximumLength(5000);
    }
}

public class SubmitContactCommandHandler : IRequestHandler<SubmitContactCommand, Guid>
{
    private readonly IApplicationDbContext _db;
    private readonly IEmailService _email;
    private readonly ILogger<SubmitContactCommandHandler> _logger;

    public SubmitContactCommandHandler(
        IApplicationDbContext db,
        IEmailService email,
        ILogger<SubmitContactCommandHandler> logger)
    {
        _db = db;
        _email = email;
        _logger = logger;
    }

    public async Task<Guid> Handle(SubmitContactCommand request, CancellationToken ct)
    {
        var message = ContactMessage.Create(request.Name, request.Email, request.Message);
        _db.ContactMessages.Add(message);
        await _db.SaveChangesAsync(ct);

        await _email.TrySendFromTemplateAsync(request.Email, "contact-confirmation", new Dictionary<string, string>
        {
            ["subject"] = "We received your message – ClaudyGod Ministry",
            ["name"] = request.Name,
            ["message"] = request.Message
        }, _logger, ct);

        return message.Id;
    }
}
