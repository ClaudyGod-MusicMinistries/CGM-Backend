using ClaudyGod.Application.Common.Interfaces;
using ClaudyGod.Application.Features.Auth.DTOs;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClaudyGod.Application.Features.Auth.Commands;

public record LoginCommand(string Email, string Password) : IRequest<TokenResponseDto>;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
    }
}

public class LoginCommandHandler : IRequestHandler<LoginCommand, TokenResponseDto>
{
    private readonly IApplicationDbContext _db;
    private readonly IJwtService _jwt;
    private readonly ICurrentUserService _currentUser;

    public LoginCommandHandler(IApplicationDbContext db, IJwtService jwt, ICurrentUserService currentUser)
    {
        _db = db;
        _jwt = jwt;
        _currentUser = currentUser;
    }

    public async Task<TokenResponseDto> Handle(LoginCommand request, CancellationToken ct)
    {
        var user = await _db.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Email == request.Email.ToLowerInvariant(), ct)
            ?? throw new Domain.Exceptions.NotFoundException("Invalid email or password.");

        if (!user.IsActive)
            throw new Domain.Exceptions.DomainException("Account is deactivated.");

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new Domain.Exceptions.NotFoundException("Invalid email or password.");

        user.RecordLogin();

        var accessToken = _jwt.GenerateAccessToken(user);
        var refreshToken = _jwt.GenerateRefreshToken(_currentUser.IpAddress);
        refreshToken.UserId = user.Id;

        user.RefreshTokens.Add(refreshToken);
        await _db.SaveChangesAsync(ct);

        return new TokenResponseDto(accessToken, refreshToken.Token,
            refreshToken.ExpiresAt, user.Role.ToString());
    }
}
