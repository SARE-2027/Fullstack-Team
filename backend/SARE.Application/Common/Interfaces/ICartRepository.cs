using SARE.Domain.Cart;

namespace SARE.Application.Common.Interfaces;

public interface ICartRepository
{
    Task<Cart?> GetByIdAsync(string cartId, CancellationToken ct = default);
    Task UpdateAsync(Cart cart, CancellationToken ct = default);
}
