using ClaudyGod.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClaudyGod.Application.Features.Auth.Commands;

public record RevokeTokenCommand(string Token) : IRequest;

public class RevokeTokenCommandHandler : IRequestHandler<RevokeTokenCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IJwtService _jwt;

    public RevokeTokenCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser,
        IJwtService jwt)
    {
        _db = db;
        _currentUser = currentUser;
        _jwt = jwt;
    }

    public async Task Handle(RevokeTokenCommand request, CancellationToken ct)
    {
        var tokenHash = _jwt.HashRefreshToken(request.Token);
        var user = await _db.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.RefreshTokens.Any(t => t.TokenHash == tokenHash), ct);

        if (user is null) return; // idempotent — already logged out

        var token = user.RefreshTokens.FirstOrDefault(t => t.TokenHash == tokenHash);
        if (token?.IsActive == true)
        {
            token.Revoke(_currentUser.IpAddress);
            await _db.SaveChangesAsync(ct);
        }
    }
}
