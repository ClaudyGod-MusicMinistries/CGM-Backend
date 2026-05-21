namespace ClaudyGod.Application.Features.Subscribers.DTOs;

public record SubscribeRequestDto(string Name, string Email);
public record UnsubscribeRequestDto(string Email, string Token);

public record SubscriberDto(Guid Id, string Name, string Email, bool IsActive, DateTime CreatedAt);
