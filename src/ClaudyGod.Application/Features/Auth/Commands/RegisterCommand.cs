using ClaudyGod.Application.Common.Interfaces;
using ClaudyGod.Application.Features.Auth.DTOs;
using ClaudyGod.Domain.Entities;
using ClaudyGod.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClaudyGod.Application.Features.Auth.Commands;

public record RegisterCommand(string Username, string Email, string Password) : IRequest<AuthResult>;

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Username).NotEmpty().MinimumLength(3).MaximumLength(50)
            .Matches("^[a-zA-Z0-9_]+$").WithMessage("Username may only contain letters, numbers, and underscores.");
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8)
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.");
    }
}

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthResult>
{
    private readonly IApplicationDbContext _db;
    private readonly IJwtService _jwt;
    private readonly ICurrentUserService _currentUser;

    public RegisterCommandHandler(IApplicationDbContext db, IJwtService jwt, ICurrentUserService currentUser)
    {
        _db = db; _jwt = jwt; _currentUser = currentUser;
    }

    public async Task<AuthResult> Handle(RegisterCommand request, CancellationToken ct)
    {
        var emailNorm = request.Email.ToLowerInvariant().Trim();

        if (await _db.Users.AnyAsync(u => u.Email == emailNorm, ct))
            throw new Domain.Exceptions.DuplicateResourceException("An account with this email already exists.");

        if (await _db.Users.AnyAsync(u => u.Username == request.Username.Trim(), ct))
            throw new Domain.Exceptions.DuplicateResourceException("Username is already taken.");

        var hash = BCrypt.Net.BCrypt.HashPassword(request.Password, workFactor: 12);
        var user = User.Create(request.Username, emailNorm, hash, UserRole.User);

        _db.Users.Add(user);

        var accessToken = _jwt.GenerateAccessToken(user);
        var refreshToken = _jwt.GenerateRefreshToken(_currentUser.IpAddress);
        refreshToken.UserId = user.Id;
        user.RefreshTokens.Add(refreshToken);

        await _db.SaveChangesAsync(ct);

        return new AuthResult(accessToken, refreshToken.Token, refreshToken.ExpiresAt, user.Role.ToString());
    }
}
