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

        try
        {
            var expected = Convert.FromHexString(storedHash);
            var computed = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
            return CryptographicOperations.FixedTimeEquals(computed, expected);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
