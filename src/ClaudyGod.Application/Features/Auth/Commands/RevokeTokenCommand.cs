using ClaudyGod.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClaudyGod.Application.Features.Auth.Commands;

public record RevokeTokenCommand(string Token) : IRequest;

public class RevokeTokenCommandHandler : IRequestHandler<RevokeTokenCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public RevokeTokenCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db; _currentUser = currentUser;
    }

    public async Task Handle(RevokeTokenCommand request, CancellationToken ct)
    {
        var user = await _db.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.RefreshTokens.Any(t => t.Token == request.Token), ct);

        if (user is null) return; // idempotent — already logged out

        var token = user.RefreshTokens.FirstOrDefault(t => t.Token == request.Token);
        if (token?.IsActive == true)
        {
            token.Revoke(_currentUser.IpAddress);
            await _db.SaveChangesAsync(ct);
        }
    }
}
