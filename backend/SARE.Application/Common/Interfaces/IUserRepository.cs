using SARE.Domain.Users;

namespace SARE.Application.Common.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByNfcUidAsync(string nfcUid, CancellationToken ct = default);
    Task<User?> GetByIdAsync(Guid userId, CancellationToken ct = default);
}
