using Microsoft.AspNetCore.SignalR;
using SARE.Application.Common.Interfaces;
using SARE.Application.Common.Security;

namespace SARE.Api.Hubs;

public class CartHub(ICartRepository carts) : Hub
{
    public async Task JoinCartGroup(string cartId, string? hardwareToken = null)
    {
        var cart = await carts.GetByIdAsync(cartId);
        if (cart is null)
        {
            throw new HubException($"العربة {cartId} غير مسجلة في النظام");
        }

        // إذا كانت العربة تمتلك رمز تحقق، نتحقق من صحة الرمز قبل السماح بالاستماع لبياناتها
        if (!string.IsNullOrWhiteSpace(cart.TokenHash))
        {
            if (string.IsNullOrWhiteSpace(hardwareToken) || !TokenHasher.VerifyToken(hardwareToken, cart.TokenHash))
            {
                throw new HubException("رمز مصادقة العربة غير صالح للاتصال بالقناة");
            }
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, $"cart_{cartId}");
    }

    public async Task LeaveCartGroup(string cartId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"cart_{cartId}");
    }
}
