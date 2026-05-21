using System.Security.Claims;
using ClaudyGod.Domain.Entities;

namespace ClaudyGod.Application.Common.Interfaces;

public interface IJwtService
{
    string GenerateAccessToken(User user);
    RefreshToken GenerateRefreshToken(string? ipAddress);
    ClaimsPrincipal? ValidateToken(string token);
}
