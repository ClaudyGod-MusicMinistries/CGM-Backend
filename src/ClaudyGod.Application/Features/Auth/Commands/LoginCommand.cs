using ClaudyGod.Application.Common.Interfaces;
using ClaudyGod.Application.Features.Auth.DTOs;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClaudyGod.Application.Features.Auth.Commands;

public record LoginCommand(string Email, string Password) : IRequest<AuthResult>;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
    }
}

public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResult>
{
    private const string DummyPasswordHash = "$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxfFP5kJ8QO.K7O3V5D7CuZU7mK";
    private readonly IApplicationDbContext _db;
    private readonly IJwtService _jwt;
    private readonly ICurrentUserService _currentUser;

    public LoginCommandHandler(IApplicationDbContext db, IJwtService jwt, ICurrentUserService currentUser)
    {
        _db = db;
        _jwt = jwt;
        _currentUser = currentUser;
    }

    public async Task<AuthResult> Handle(LoginCommand request, CancellationToken ct)
    {
        var user = await _db.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Email == request.Email.ToLowerInvariant().Trim(), ct);

        if (user is null)
        {
            BCrypt.Net.BCrypt.Verify(request.Password, DummyPasswordHash);
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password.");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("Invalid email or password.");

        // Revoke all tokens from same IP to enforce single-session-per-device
        var staleTokens = user.RefreshTokens
            .Where(t => t.IsActive && t.CreatedByIp == _currentUser.IpAddress)
            .ToList();
        foreach (var t in staleTokens)
            t.Revoke(_currentUser.IpAddress);

        user.RecordLogin();

        var accessToken = _jwt.GenerateAccessToken(user);
        var refreshToken = _jwt.GenerateRefreshToken(_currentUser.IpAddress);
        refreshToken.Entity.UserId = user.Id;
        user.RefreshTokens.Add(refreshToken.Entity);

        await _db.SaveChangesAsync(ct);

        return new AuthResult(accessToken.Token, accessToken.ExpiresAt,
            refreshToken.PlainTextToken, refreshToken.Entity.ExpiresAt, user.Role.ToString());
    }
}
