using ClaudyGod.Application.Common.Interfaces;
using FluentValidation;
using MediatR;

namespace ClaudyGod.Application.Features.AI.Queries;

public record PrayerChatQuery(string Message) : IRequest<string>;

public class PrayerChatQueryValidator : AbstractValidator<PrayerChatQuery>
{
    public PrayerChatQueryValidator()
    {
        RuleFor(x => x.Message).NotEmpty().MaximumLength(2000);
    }
}

public class PrayerChatQueryHandler : IRequestHandler<PrayerChatQuery, string>
{
    private readonly IAIService _ai;

    public PrayerChatQueryHandler(IAIService ai) => _ai = ai;

    public Task<string> Handle(PrayerChatQuery request, CancellationToken ct) =>
        _ai.ChatAsync(request.Message, null, AIPersona.PrayerCompanion, ct);
}
