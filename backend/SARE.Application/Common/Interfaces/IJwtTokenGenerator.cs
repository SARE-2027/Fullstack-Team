using SARE.Domain.Users;

namespace SARE.Application.Common.Interfaces;

public interface IJwtTokenGenerator
{
    (string Token, DateTime ExpiresAtUtc) GenerateAccessToken(User user, IEnumerable<string> roles);
    (string Token, DateTime ExpiresAtUtc) GenerateRefreshToken();
}
