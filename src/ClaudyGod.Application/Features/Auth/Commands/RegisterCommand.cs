using ClaudyGod.Application.Common.Interfaces;
using ClaudyGod.Application.Features.Auth.DTOs;
using ClaudyGod.Domain.Entities;
using ClaudyGod.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClaudyGod.Application.Features.Auth.Commands;

public record RegisterCommand(string Username, string Email, string Password) : IRequest<TokenResponseDto>;

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Username).NotEmpty().MinimumLength(3).MaximumLength(50);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8)
            .Matches("[A-Z]").WithMessage("Password must contain an uppercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain a number.");
    }
}

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, TokenResponseDto>
{
    private readonly IApplicationDbContext _db;
    private readonly IJwtService _jwt;
    private readonly ICurrentUserService _currentUser;

    public RegisterCommandHandler(IApplicationDbContext db, IJwtService jwt, ICurrentUserService currentUser)
    {
        _db = db; _jwt = jwt; _currentUser = currentUser;
    }

    public async Task<TokenResponseDto> Handle(RegisterCommand request, CancellationToken ct)
    {
        var exists = await _db.Users.AnyAsync(u => u.Email == request.Email.ToLowerInvariant(), ct);
        if (exists) throw new Domain.Exceptions.DuplicateResourceException("Email already registered.");

        var hash = BCrypt.Net.BCrypt.HashPassword(request.Password, 12);
        var user = User.Create(request.Username, request.Email, hash, UserRole.User);

        _db.Users.Add(user);

        var accessToken = _jwt.GenerateAccessToken(user);
        var refreshToken = _jwt.GenerateRefreshToken(_currentUser.IpAddress);
        refreshToken.UserId = user.Id;
        user.RefreshTokens.Add(refreshToken);

        await _db.SaveChangesAsync(ct);

        return new TokenResponseDto(accessToken, refreshToken.Token,
            refreshToken.ExpiresAt, user.Role.ToString());
    }
}
