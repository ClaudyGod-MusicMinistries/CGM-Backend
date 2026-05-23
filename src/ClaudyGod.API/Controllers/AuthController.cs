using ClaudyGod.Application.Common.Models;
using ClaudyGod.Application.Features.Auth.Commands;
using ClaudyGod.Application.Features.Auth.DTOs;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ClaudyGod.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator) => _mediator = mediator;

    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<TokenResponseDto>>> Register(
        [FromBody] RegisterRequestDto dto, CancellationToken ct)
    {
        var result = await _mediator.Send(new RegisterCommand(dto.Username, dto.Email, dto.Password), ct);
        return Ok(ApiResponse<TokenResponseDto>.Ok(result, "Registration successful."));
    }

    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<TokenResponseDto>>> Login(
        [FromBody] LoginRequestDto dto, CancellationToken ct)
    {
        var result = await _mediator.Send(new LoginCommand(dto.Email, dto.Password), ct);
        return Ok(ApiResponse<TokenResponseDto>.Ok(result, "Login successful."));
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<ApiResponse<TokenResponseDto>>> Refresh(
        [FromBody] RefreshTokenRequestDto dto, CancellationToken ct)
    {
        var result = await _mediator.Send(new RefreshTokenCommand(dto.RefreshToken), ct);
        return Ok(ApiResponse<TokenResponseDto>.Ok(result));
    }
}
