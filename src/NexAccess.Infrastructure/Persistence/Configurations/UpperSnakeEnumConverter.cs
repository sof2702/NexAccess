using System.Text;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace NexAccess.Infrastructure.Persistence.Configurations;

public sealed class UpperSnakeEnumConverter<TEnum> : ValueConverter<TEnum, string>
    where TEnum : struct, Enum
{
    public UpperSnakeEnumConverter()
        : base(value => ToDatabase(value), value => FromDatabase(value))
    {
    }

    public static string ToDatabase(TEnum value)
    {
        var name = value.ToString();
        var builder = new StringBuilder(name.Length + 4);
        for (var index = 0; index < name.Length; index++)
        {
            if (index > 0 && char.IsUpper(name[index]) && char.IsLower(name[index - 1]))
            {
                builder.Append('_');
            }

            builder.Append(char.ToUpperInvariant(name[index]));
        }

        return builder.ToString();
    }

    public static TEnum FromDatabase(string value) =>
        Enum.Parse<TEnum>(value.Replace("_", string.Empty), ignoreCase: true);
}
