namespace ClaudyGod.Application.Features.Auth.DTOs;

public record LoginRequestDto(string Email, string Password);
public record RegisterRequestDto(string Username, string Email, string Password);

// Returned to the client — refresh token is delivered via HTTP-only cookie, never in the body
public record AuthResponseDto(string AccessToken, string Role, DateTime AccessTokenExpiresAt);

// Internal result carried from command handler → controller
public record AuthResult(string AccessToken, string RefreshToken, DateTime RefreshTokenExpiresAt, string Role);

public record ResetPasswordRequestDto(string Email);
public record ConfirmResetPasswordDto(string Token, string NewPassword);
