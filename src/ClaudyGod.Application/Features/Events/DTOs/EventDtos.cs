namespace ClaudyGod.Application.Features.Events.DTOs;

public record CreateEventRequest(
    string Title,
    string? Description,
    string? Venue,
    DateTime StartDate,
    DateTime? EndDate,
    int TotalCapacity,
    bool IsFree = true,
    decimal? TicketPrice = null,
    string? AddressLine1 = null,
    string? City = null,
    string? State = null,
    string? Country = null,
    string? ZipCode = null);

public record EventDto(
    Guid Id,
    string Title,
    string? Description,
    string? Venue,
    DateTime StartDate,
    DateTime? EndDate,
    int TotalCapacity,
    int ReservedCount,
    int AvailableSeats,
    bool IsFree,
    decimal? TicketPrice,
    string Status,
    string? FlyerImagePath,
    DateTime CreatedAt);

public record EventDetailDto(
    Guid Id,
    string Title,
    string? Description,
    string? Venue,
    DateTime StartDate,
    DateTime? EndDate,
    int TotalCapacity,
    int ReservedCount,
    int AvailableSeats,
    bool IsFree,
    decimal? TicketPrice,
    string Status,
    string? FlyerImagePath,
    string? LocationCity,
    string? LocationState,
    string? LocationCountry,
    DateTime CreatedAt);
