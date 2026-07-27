using ClaudyGod.Application.Common.Interfaces;
using ClaudyGod.Application.Features.Auth.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClaudyGod.Application.Features.Auth.Commands;

public record RefreshTokenCommand(string Token) : IRequest<AuthResult>;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthResult>
{
    private readonly IApplicationDbContext _db;
    private readonly IJwtService _jwt;
    private readonly ICurrentUserService _currentUser;

    public RefreshTokenCommandHandler(IApplicationDbContext db, IJwtService jwt, ICurrentUserService currentUser)
    {
        _db = db; _jwt = jwt; _currentUser = currentUser;
    }

    public async Task<AuthResult> Handle(RefreshTokenCommand request, CancellationToken ct)
    {
        var tokenHash = _jwt.HashRefreshToken(request.Token);
        var user = await _db.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.RefreshTokens.Any(t => t.TokenHash == tokenHash), ct)
            ?? throw new UnauthorizedAccessException("Invalid refresh token.");

        var oldToken = user.RefreshTokens.First(t => t.TokenHash == tokenHash);

        if (!oldToken.IsActive)
        {
            // Token reuse detected — revoke entire family (all active tokens for this user)
            foreach (var t in user.RefreshTokens.Where(t => t.IsActive))
                t.Revoke(_currentUser.IpAddress);
            await _db.SaveChangesAsync(ct);
            throw new Domain.Exceptions.DomainException("Refresh token has been revoked. Please log in again.");
        }

        // Rotate: revoke old, issue new
        oldToken.Revoke(_currentUser.IpAddress);

        var newAccessToken = _jwt.GenerateAccessToken(user);
        var newRefreshToken = _jwt.GenerateRefreshToken(_currentUser.IpAddress);
        newRefreshToken.Entity.UserId = user.Id;
        user.RefreshTokens.Add(newRefreshToken.Entity);

        await _db.SaveChangesAsync(ct);

        return new AuthResult(newAccessToken.Token, newAccessToken.ExpiresAt,
            newRefreshToken.PlainTextToken, newRefreshToken.Entity.ExpiresAt, user.Role.ToString());
    }
}
