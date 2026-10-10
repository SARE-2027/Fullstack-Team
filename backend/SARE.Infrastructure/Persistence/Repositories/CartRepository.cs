using Microsoft.EntityFrameworkCore;
using SARE.Application.Common.Interfaces;
using SARE.Domain.Cart;

namespace SARE.Infrastructure.Persistence.Repositories;

public class CartRepository(AppDbContext db) : ICartRepository
{
    public async Task<Cart?> GetByIdAsync(string cartId, CancellationToken ct = default)
        => await db.Carts.FirstOrDefaultAsync(c => c.Id == cartId, ct);

    public async Task UpdateAsync(Cart cart, CancellationToken ct = default)
    {
        db.Carts.Update(cart);
        await db.SaveChangesAsync(ct);
    }
}