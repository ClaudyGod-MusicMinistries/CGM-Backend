using System.Security.Claims;
using Asp.Versioning;
using ClaudyGod.API.Attributes;
using ClaudyGod.Application.Common.Models;
using ClaudyGod.Application.Features.Auth.Commands;
using ClaudyGod.Application.Features.Auth.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClaudyGod.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
[EnableRateLimiting("auth")]
public sealed class AuthController : ControllerBase
{
    private const string RefreshCookieName = "cgm_refresh_token";
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator) => _mediator = mediator;

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Register(
        [FromBody] RegisterRequestDto request, CancellationToken ct)
    {
        var result = await _mediator.Send(
            new RegisterCommand(request.Username, request.Email, request.Password), ct);
        SetRefreshCookie(result);
        return Ok(ApiResponse<AuthResponseDto>.Ok(ToResponse(result), "Registration successful."));
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Login(
        [FromBody] LoginRequestDto request, CancellationToken ct)
    {
        var result = await _mediator.Send(new LoginCommand(request.Email, request.Password), ct);
        SetRefreshCookie(result);
        return Ok(ApiResponse<AuthResponseDto>.Ok(ToResponse(result), "Login successful."));
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Refresh(CancellationToken ct)
    {
        if (!Request.Cookies.TryGetValue(RefreshCookieName, out var token) || string.IsNullOrWhiteSpace(token))
            throw new UnauthorizedAccessException("A refresh token is required.");

        var result = await _mediator.Send(new RefreshTokenCommand(token), ct);
        SetRefreshCookie(result);
        return Ok(ApiResponse<AuthResponseDto>.Ok(ToResponse(result), "Token refreshed."));
    }

    [AllowAnonymous]
    [HttpPost("logout")]
    public async Task<ActionResult<ApiResponse>> Logout(CancellationToken ct)
    {
        if (Request.Cookies.TryGetValue(RefreshCookieName, out var token) && !string.IsNullOrWhiteSpace(token))
            await _mediator.Send(new RevokeTokenCommand(token), ct);

        Response.Cookies.Delete(RefreshCookieName, RefreshCookieOptions(DateTimeOffset.UnixEpoch));
        return Ok(ApiResponse.Ok("Logged out."));
    }

    [Authorize]
    [HttpGet("me")]
    public ActionResult<ApiResponse<object>> Me() => Ok(ApiResponse<object>.Ok(new
    {
        id = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"),
        email = User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email"),
        username = User.FindFirstValue(ClaimTypes.Name) ?? User.FindFirstValue("unique_name"),
        role = User.FindFirstValue(ClaimTypes.Role),
    }));

    private void SetRefreshCookie(AuthResult result) =>
        Response.Cookies.Append(RefreshCookieName, result.RefreshToken,
            RefreshCookieOptions(result.RefreshTokenExpiresAt));

    private static AuthResponseDto ToResponse(AuthResult result) =>
        new(result.AccessToken, result.Role, result.AccessTokenExpiresAt);

    private static CookieOptions RefreshCookieOptions(DateTimeOffset expires) => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Expires = expires,
        Path = "/api/v1.0/auth",
        IsEssential = true,
    };
}
