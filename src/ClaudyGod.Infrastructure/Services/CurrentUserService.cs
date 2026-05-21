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

    public string? UserId => User?.FindFirstValue(ClaimTypes.NameIdentifier)
                          ?? User?.FindFirstValue("sub");

    public string? UserEmail => User?.FindFirstValue(ClaimTypes.Email)
                             ?? User?.FindFirstValue("email");

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

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;
}
