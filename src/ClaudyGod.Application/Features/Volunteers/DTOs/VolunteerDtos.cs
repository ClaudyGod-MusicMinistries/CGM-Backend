using ClaudyGod.Domain.Enums;

namespace ClaudyGod.Application.Features.Volunteers.DTOs;

public record RegisterVolunteerRequest(
    string FirstName,
    string LastName,
    string Email,
    VolunteerRole Role,
    string Reason);

public record VolunteerDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string Role,
    string Reason,
    bool IsApproved,
    DateTime? ApprovedAt,
    DateTime CreatedAt);
