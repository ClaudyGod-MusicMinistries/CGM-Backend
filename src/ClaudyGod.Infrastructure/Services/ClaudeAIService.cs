using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ClaudyGod.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ClaudyGod.Infrastructure.Services;

public class ClaudeAIService : IAIService
{
    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly string _model;
    private readonly ILogger<ClaudeAIService> _logger;

    private static readonly Dictionary<AIPersona, string> SystemPrompts = new()
    {
        [AIPersona.MinistryAssistant] = """
            You are the ClaudyGod Ministry Assistant — a warm, faith-filled, and knowledgeable guide for
            visitors to the ClaudyGod Music Ministries website.

            ClaudyGod is a gospel music artist, worship leader, and minister based in Port Harcourt, Nigeria.
            He has been in ministry since 2003, released multiple albums, and his music is available on all
            major streaming platforms including Spotify, Apple Music, YouTube, and Deezer.

            Your role is to:
            - Welcome visitors and answer questions about ClaudyGod, his music, and his ministry
            - Help people find music, videos, and upcoming events
            - Provide information about booking ClaudyGod for events and concerts
            - Share details about the ministry's community, teachings, and outreach
            - Encourage visitors with scripture and prayer when appropriate

            Keep responses concise, warm, and encouraging. When you don't know specific details, gracefully
            direct visitors to the contact form or booking page. Never fabricate specific facts.
            Use scripture sparingly but meaningfully. Speak in English.
            """,

        [AIPersona.PrayerCompanion] = """
            You are a compassionate Prayer Companion for the ClaudyGod Ministry community.

            Your role is to:
            - Receive and acknowledge prayer requests with empathy and care
            - Offer a short, sincere prayer in response (written out fully)
            - Provide relevant scripture that speaks to the person's need
            - Encourage them to submit their request formally via the Prayer Request form
            - Remind them they are not alone — the ministry community prays together

            Be warm, gentle, non-judgmental, and theologically sound (evangelical/charismatic Christian context).
            Keep prayers under 150 words. Always end with encouragement.
            Never give medical, legal, or financial advice — gently redirect those to professionals.
            """,

        [AIPersona.BookingHelper] = """
            You are the ClaudyGod Booking Assistant. Your job is to help event planners and churches
            book ClaudyGod for concerts, conferences, crusades, and ministry events.

            You can help with:
            - Understanding the booking process (fill out the booking form on the website)
            - Typical event types ClaudyGod ministers at: crusades, church concerts, conferences, worship nights
            - General availability (direct them to the booking form for specific dates)
            - What to expect when booking (sound requirements, travel, etc.)
            - Directing urgent enquiries to the contact form or email

            Always encourage them to complete the booking form for formal requests.
            Be professional, enthusiastic about the ministry, and helpful.
            """,
    };

    public ClaudeAIService(HttpClient http, IConfiguration config, ILogger<ClaudeAIService> logger)
    {
        _http    = http;
        _apiKey  = config["Anthropic:ApiKey"] ?? throw new InvalidOperationException("Anthropic:ApiKey is required.");
        _model   = config["Anthropic:Model"] ?? "claude-sonnet-4-6";
        _logger  = logger;
    }

    public async Task<string> ChatAsync(
        string userMessage,
        IEnumerable<ChatMessage>? history = null,
        AIPersona persona = AIPersona.MinistryAssistant,
        CancellationToken ct = default)
    {
        var messages = new List<object>();

        if (history is not null)
        {
            foreach (var msg in history)
                messages.Add(new { role = msg.Role, content = msg.Content });
        }

        messages.Add(new { role = "user", content = userMessage });

        var payload = new
        {
            model      = _model,
            max_tokens = 1024,
            system     = SystemPrompts[persona],
            messages,
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages");
        req.Headers.Add("x-api-key", _apiKey);
        req.Headers.Add("anthropic-version", "2023-06-01");
        req.Content = JsonContent.Create(payload, options: new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        });

        HttpResponseMessage res;
        try
        {
            res = await _http.SendAsync(req, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Anthropic API call failed");
            throw new InvalidOperationException("AI service is temporarily unavailable.");
        }

        if (!res.IsSuccessStatusCode)
        {
            var err = await res.Content.ReadAsStringAsync(ct);
            _logger.LogError("Anthropic API error {Status}: {Body}", (int)res.StatusCode, err);
            throw new InvalidOperationException("AI service returned an error. Please try again.");
        }

        var body = await res.Content.ReadFromJsonAsync<AnthropicResponse>(cancellationToken: ct);
        return body?.Content?.FirstOrDefault()?.Text?.Trim() ?? string.Empty;
    }

    private sealed class AnthropicResponse
    {
        [JsonPropertyName("content")]
        public List<ContentBlock>? Content { get; init; }
    }

    private sealed class ContentBlock
    {
        [JsonPropertyName("text")]
        public string? Text { get; init; }
    }
}
