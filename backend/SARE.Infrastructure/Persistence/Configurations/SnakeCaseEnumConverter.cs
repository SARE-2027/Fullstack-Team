using System.Text;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace SARE.Infrastructure.Persistence.Configurations;

internal sealed class SnakeCaseEnumConverter<TEnum>()
    : ValueConverter<TEnum, string>(value => ToDatabase(value), value => FromDatabase(value))
    where TEnum : struct, Enum
{
    private static string ToDatabase(TEnum value)
    {
        var name = value.ToString();
        var result = new StringBuilder(name.Length + 4);

        for (var index = 0; index < name.Length; index++)
        {
            var character = name[index];
            if (char.IsUpper(character) && index > 0)
            {
                result.Append('_');
            }

            result.Append(char.ToLowerInvariant(character));
        }

        return result.ToString();
    }

    private static TEnum FromDatabase(string value) =>
        Enum.Parse<TEnum>(value.Replace("_", string.Empty), ignoreCase: true);
}
