using ClaudyGod.Domain.Enums;

namespace ClaudyGod.Application.Common.Interfaces;

public interface ICurrentUserService
{
    string? UserId { get; }
    string? UserEmail { get; }
    UserRole? UserRole { get; }
    string? IpAddress { get; }
    bool IsAuthenticated { get; }
}
