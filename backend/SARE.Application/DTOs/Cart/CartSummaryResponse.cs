using System.Globalization;

namespace SARE.Application.DTOs.Cart;

public record CartSummaryResponse(
    Guid SessionId,
    string CartId,
    Guid? UserId,
    string? UserName,
    string Status,
    int TotalMinor,
    int ItemsCount,
    DateTime StartedAt,
    IReadOnlyList<SessionItemDto> Items
)
{
    public DateTime UpdatedAt { get; init; }
    // A string preserves timestamp precision in JavaScript; compare versions with BigInt.
    public string Version => UpdatedAt.Ticks.ToString(CultureInfo.InvariantCulture);
    public DateTime? ClosedAt { get; init; }
    public Guid? ClosedByUserId { get; init; }
    public string? CloseReason { get; init; }
}
