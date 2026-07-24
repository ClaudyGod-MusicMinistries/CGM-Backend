using System.Security.Claims;
using ClaudyGod.Application.Common.Interfaces;
using ClaudyGod.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace ClaudyGod.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    // Every admin-write endpoint is now protected solely by ApiKeyMiddleware's
    // x-api-key check (see ApiKeyMiddleware, [Authorize] was removed sitewide) —
    // there's no longer a JWT principal minted by this API's own auth. The trusted
    // caller (the admin portal's services/api, which does its own real per-user
    // RBAC before ever reaching here) forwards who's actually acting via
    // x-actor-id/x-actor-email headers so AuditLog still records a real actor
    // instead of falling back to "anonymous" for every write.
    private string? ActorIdHeader =>
        _httpContextAccessor.HttpContext?.Request.Headers["x-actor-id"].FirstOrDefault();

    private string? ActorEmailHeader =>
        _httpContextAccessor.HttpContext?.Request.Headers["x-actor-email"].FirstOrDefault();

    public string? UserId => User?.FindFirstValue(ClaimTypes.NameIdentifier)
                          ?? User?.FindFirstValue("sub")
                          ?? ActorIdHeader;

    public string? UserEmail => User?.FindFirstValue(ClaimTypes.Email)
                             ?? User?.FindFirstValue("email")
                             ?? ActorEmailHeader;

    public UserRole? UserRole
    {
        get
        {
            var role = User?.FindFirstValue(ClaimTypes.Role);
            return Enum.TryParse<UserRole>(role, out var r) ? r : null;
        }
    }

    public string? IpAddress =>
        _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    public bool IsAuthenticated =>
        (User?.Identity?.IsAuthenticated ?? false) || !string.IsNullOrEmpty(ActorIdHeader);
}
