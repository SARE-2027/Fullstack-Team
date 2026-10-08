namespace SARE.Application.DTOs.Auth;

public sealed record RefreshTokenRequest(
    string AccessToken,
    string RefreshToken);
