namespace SARE.Application.Common.Interfaces;

public interface ICartNotificationService
{
    Task NotifySessionStartedAsync(string cartId, Guid sessionId);
    Task NotifyCartUpdatedAsync(string cartId, Guid sessionId, int totalMinor, int itemCount, string latestItemName);
    Task NotifyFraudAlertAsync(string cartId, Guid sessionId, string barcode, int weightDiffG, string message);
    Task NotifyCheckoutSuccessAsync(string cartId, Guid sessionId, int totalPaidMinor, string receiptHash);
}
