using System.Security.Claims;
using ClaudyGod.Domain.Entities;

namespace ClaudyGod.Application.Common.Interfaces;

public interface IJwtService
{
    AccessTokenResult GenerateAccessToken(User user);
    RefreshToken GenerateRefreshToken(string? ipAddress);
    ClaimsPrincipal? ValidateToken(string token);
}

public sealed record AccessTokenResult(string Token, DateTime ExpiresAt);
