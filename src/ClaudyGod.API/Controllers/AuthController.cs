using Asp.Versioning;
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
public class AuthController : ControllerBase
{
    private const string RefreshTokenCookie = "cgm_rt";
    private readonly IMediator _mediator;
    private readonly IConfiguration _config;

    public AuthController(IMediator mediator, IConfiguration config)
    {
        _mediator = mediator;
        _config = config;
    }

    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Register(
        [FromBody] RegisterRequestDto dto, CancellationToken ct)
    {
        var result = await _mediator.Send(new RegisterCommand(dto.Username, dto.Email, dto.Password), ct);
        SetRefreshTokenCookie(result.RefreshToken, result.RefreshTokenExpiresAt);
        return Ok(ApiResponse<AuthResponseDto>.Ok(ToResponse(result), "Registration successful."));
    }

    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Login(
        [FromBody] LoginRequestDto dto, CancellationToken ct)
    {
        var result = await _mediator.Send(new LoginCommand(dto.Email, dto.Password), ct);
        SetRefreshTokenCookie(result.RefreshToken, result.RefreshTokenExpiresAt);
        return Ok(ApiResponse<AuthResponseDto>.Ok(ToResponse(result), "Login successful."));
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Refresh(CancellationToken ct)
    {
        var refreshToken = Request.Cookies[RefreshTokenCookie];
        if (string.IsNullOrEmpty(refreshToken))
            return Unauthorized(ApiResponse.Fail("Session expired. Please log in again."));

        var result = await _mediator.Send(new RefreshTokenCommand(refreshToken), ct);
        SetRefreshTokenCookie(result.RefreshToken, result.RefreshTokenExpiresAt);
        return Ok(ApiResponse<AuthResponseDto>.Ok(ToResponse(result)));
    }

    [HttpPost("logout")]
    public async Task<ActionResult<ApiResponse>> Logout(CancellationToken ct)
    {
        var refreshToken = Request.Cookies[RefreshTokenCookie];
        if (!string.IsNullOrEmpty(refreshToken))
            await _mediator.Send(new RevokeTokenCommand(refreshToken), ct);

        ClearRefreshTokenCookie();
        return Ok(ApiResponse.Ok("Logged out successfully."));
    }

    [Authorize]
    [HttpGet("me")]
    public ActionResult<ApiResponse<object>> Me()
    {
        var userId   = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                    ?? User.FindFirst("sub")?.Value;
        var email    = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
                    ?? User.FindFirst("email")?.Value;
        var username = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value
                    ?? User.FindFirst("unique_name")?.Value;
        var role     = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

        return Ok(ApiResponse<object>.Ok(new { id = userId, email, username, role }));
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private static AuthResponseDto ToResponse(AuthResult result)
    {
        var expiryMinutes = 60;
        return new AuthResponseDto(result.AccessToken, result.Role,
            DateTime.UtcNow.AddMinutes(expiryMinutes));
    }

    private void SetRefreshTokenCookie(string token, DateTime expiresAt)
    {
        var isProd = HttpContext.RequestServices
            .GetRequiredService<IWebHostEnvironment>().IsProduction();

        Response.Cookies.Append(RefreshTokenCookie, token, new CookieOptions
        {
            HttpOnly  = true,
            Secure    = isProd,                // HTTPS-only in production
            SameSite  = SameSiteMode.Strict,   // CSRF protection
            Expires   = expiresAt,
            Path      = "/api/",               // scoped to API paths only
        });
    }

    private void ClearRefreshTokenCookie() =>
        Response.Cookies.Delete(RefreshTokenCookie, new CookieOptions
        {
            HttpOnly = true,
            Secure   = HttpContext.RequestServices
                           .GetRequiredService<IWebHostEnvironment>().IsProduction(),
            SameSite = SameSiteMode.Strict,
            Path     = "/api/",
        });
}
