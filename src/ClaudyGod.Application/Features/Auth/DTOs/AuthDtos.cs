namespace ClaudyGod.Application.Features.Auth.DTOs;

public record LoginRequestDto(string Email, string Password);
public record RegisterRequestDto(string Username, string Email, string Password);
public record TokenResponseDto(string AccessToken, string RefreshToken, DateTime ExpiresAt, string Role);
public record RefreshTokenRequestDto(string RefreshToken);
public record ResetPasswordRequestDto(string Email);
public record ConfirmResetPasswordDto(string Token, string NewPassword);
