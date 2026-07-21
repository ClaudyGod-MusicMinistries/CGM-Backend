using ClaudyGod.Application.Common.Interfaces;
using FluentValidation;
using MediatR;

namespace ClaudyGod.Application.Features.AI.Queries;

public record BookingHelpChatQuery(string Message) : IRequest<string>;

public class BookingHelpChatQueryValidator : AbstractValidator<BookingHelpChatQuery>
{
    public BookingHelpChatQueryValidator()
    {
        RuleFor(x => x.Message).NotEmpty().MaximumLength(1000);
    }
}

public class BookingHelpChatQueryHandler : IRequestHandler<BookingHelpChatQuery, string>
{
    private readonly IAIService _ai;

    public BookingHelpChatQueryHandler(IAIService ai) => _ai = ai;

    public Task<string> Handle(BookingHelpChatQuery request, CancellationToken ct) =>
        _ai.ChatAsync(request.Message, null, AIPersona.BookingHelper, ct);
}
