namespace SARE.Application.DTOs.Auth;

public sealed record UserDto(
    Guid Id,
    string Name,
    string Email,
    string? NfcUid,
    bool IsActive,
    IReadOnlyList<string> Roles,
    DateTime CreatedAt);
