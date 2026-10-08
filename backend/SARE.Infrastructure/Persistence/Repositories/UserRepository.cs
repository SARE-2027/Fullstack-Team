using Microsoft.EntityFrameworkCore;
using SARE.Application.Common.Interfaces;
using SARE.Domain.Users;

namespace SARE.Infrastructure.Persistence.Repositories;

public class UserRepository(AppDbContext db) : IUserRepository
{
    public async Task<User?> GetByNfcUidAsync(string nfcUid, CancellationToken ct = default)
        => await db.Users.FirstOrDefaultAsync(u => u.NfcUid == nfcUid, ct);

    public async Task<User?> GetByIdAsync(Guid userId, CancellationToken ct = default)
        => await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
}
