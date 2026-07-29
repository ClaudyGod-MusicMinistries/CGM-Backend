using System.Security.Claims;
using ClaudyGod.Domain.Entities;

namespace ClaudyGod.Application.Common.Interfaces;

public interface IJwtService
{
    AccessTokenResult GenerateAccessToken(User user);
    RefreshTokenResult GenerateRefreshToken(string? ipAddress);
    string HashRefreshToken(string token);
    ClaimsPrincipal? ValidateToken(string token);
}

public sealed record AccessTokenResult(string Token, DateTime ExpiresAt);
public sealed record RefreshTokenResult(string PlainTextToken, RefreshToken Entity);
