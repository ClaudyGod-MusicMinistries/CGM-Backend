using ClaudyGod.Application.Common.Interfaces;
using FluentValidation;
using MediatR;

namespace ClaudyGod.Application.Features.AI.Queries;

public record ChatMessageDto(string Role, string Content);

public record ChatWithAssistantQuery(string Message, List<ChatMessageDto>? History) : IRequest<string>;

public class ChatWithAssistantQueryValidator : AbstractValidator<ChatWithAssistantQuery>
{
    public ChatWithAssistantQueryValidator()
    {
        RuleFor(x => x.Message).NotEmpty().MaximumLength(1000);
    }
}

public class ChatWithAssistantQueryHandler : IRequestHandler<ChatWithAssistantQuery, string>
{
    private readonly IAIService _ai;

    public ChatWithAssistantQueryHandler(IAIService ai) => _ai = ai;

    public Task<string> Handle(ChatWithAssistantQuery request, CancellationToken ct)
    {
        var history = request.History?
            .Take(10) // limit context window to last 10 turns
            .Select(m => new ChatMessage(m.Role, m.Content));

        return _ai.ChatAsync(request.Message, history, AIPersona.MinistryAssistant, ct);
    }
}
