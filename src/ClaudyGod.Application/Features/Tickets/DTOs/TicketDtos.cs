namespace ClaudyGod.Application.Features.Tickets.DTOs;

public record ReserveTicketRequest(
    Guid EventId,
    string FirstName,
    string LastName,
    string Email,
    string Phone,
    int Quantity = 1);

public record TicketDto(
    Guid Id,
    Guid EventId,
    string EventTitle,
    string AttendeeFirstName,
    string AttendeeLastName,
    string AttendeeEmail,
    int Quantity,
    string ConfirmationCode,
    string Status,
    DateTime? CheckedInAt,
    DateTime CreatedAt);
