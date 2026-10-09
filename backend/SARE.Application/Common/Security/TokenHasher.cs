using System.Security.Cryptography;
using System.Text;

namespace SARE.Application.Common.Security;

public static class TokenHasher
{
    public static string HashToken(string rawToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexStringLower(bytes);
    }

    public static bool VerifyToken(string rawToken, string storedHash)
    {
        if (string.IsNullOrWhiteSpace(rawToken) || string.IsNullOrWhiteSpace(storedHash))
            return false;

        var computed = HashToken(rawToken);
        return string.Equals(computed, storedHash, StringComparison.OrdinalIgnoreCase)
            || string.Equals(rawToken, storedHash, StringComparison.OrdinalIgnoreCase);
    }
}
