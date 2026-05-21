using ClaudyGod.Application.Common.Interfaces;
using ClaudyGod.Application.Features.Auth.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClaudyGod.Application.Features.Auth.Commands;

public record RefreshTokenCommand(string Token) : IRequest<TokenResponseDto>;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, TokenResponseDto>
{
    private readonly IApplicationDbContext _db;
    private readonly IJwtService _jwt;
    private readonly ICurrentUserService _currentUser;

    public RefreshTokenCommandHandler(IApplicationDbContext db, IJwtService jwt, ICurrentUserService currentUser)
    {
        _db = db; _jwt = jwt; _currentUser = currentUser;
    }

    public async Task<TokenResponseDto> Handle(RefreshTokenCommand request, CancellationToken ct)
    {
        var user = await _db.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.RefreshTokens.Any(t => t.Token == request.Token), ct)
            ?? throw new Domain.Exceptions.NotFoundException("Invalid refresh token.");

        var oldToken = user.RefreshTokens.First(t => t.Token == request.Token);
        if (!oldToken.IsActive)
            throw new Domain.Exceptions.DomainException("Refresh token is expired or revoked.");

        oldToken.Revoke(_currentUser.IpAddress);

        var newAccessToken = _jwt.GenerateAccessToken(user);
        var newRefreshToken = _jwt.GenerateRefreshToken(_currentUser.IpAddress);
        newRefreshToken.UserId = user.Id;
        user.RefreshTokens.Add(newRefreshToken);

        await _db.SaveChangesAsync(ct);

        return new TokenResponseDto(newAccessToken, newRefreshToken.Token,
            newRefreshToken.ExpiresAt, user.Role.ToString());
    }
}
