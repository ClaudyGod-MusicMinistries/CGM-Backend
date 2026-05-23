namespace ClaudyGod.Application.Common.Interfaces;

public interface IAIService
{
    /// <summary>Send a user message in a given conversation context and return the assistant reply.</summary>
    Task<string> ChatAsync(string userMessage, IEnumerable<ChatMessage>? history = null,
        AIPersona persona = AIPersona.MinistryAssistant, CancellationToken ct = default);
}

public enum AIPersona
{
    MinistryAssistant,
    PrayerCompanion,
    BookingHelper,
}

public record ChatMessage(string Role, string Content);
