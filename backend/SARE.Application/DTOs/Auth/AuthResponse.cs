namespace SARE.Application.DTOs.Auth;

public sealed record AuthResponse(
    Guid Id,
    string Name,
    string Email,
    IReadOnlyList<string> Roles,
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAtUtc);
