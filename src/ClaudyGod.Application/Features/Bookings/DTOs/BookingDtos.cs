using ClaudyGod.Domain.Enums;

namespace ClaudyGod.Application.Features.Bookings.DTOs;

public record CreateBookingRequest(
    string FirstName,
    string LastName,
    string Email,
    string Phone,
    CountryCode CountryCode,
    string Organization,
    string OrgType,
    string EventType,
    string EventDetails,
    DateTime EventDate,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string State,
    string ZipCode,
    string Country,
    bool AgreeTerms);

public record BookingDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string Phone,
    string Organization,
    string EventType,
    string EventDetails,
    DateTime EventDate,
    string Status,
    string? AdminNotes,
    DateTime CreatedAt);

public record UpdateBookingStatusRequest(string Status, string? AdminNotes);
