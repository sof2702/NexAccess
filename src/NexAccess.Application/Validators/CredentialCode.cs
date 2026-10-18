using System.Text.RegularExpressions;

namespace NexAccess.Application.Validators;

public static partial class CredentialCode
{
    public const int MinLength = 4;
    public const int MaxLength = 150;

    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return SeparatorPattern().Replace(value.Trim(), string.Empty).ToUpperInvariant();
    }

    public static bool IsValid(string normalized) =>
        normalized.Length is >= MinLength and <= MaxLength && AllowedPattern().IsMatch(normalized);

    public static bool TryNormalize(string? value, out string normalized)
    {
        normalized = Normalize(value);
        return IsValid(normalized);
    }

    public static string Mask(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value.Length <= 4 ? new string('*', value.Length) : new string('*', value.Length - 4) + value[^4..];
    }

    [GeneratedRegex(@"[\s:\-]")]
    private static partial Regex SeparatorPattern();

    [GeneratedRegex("^[A-Z0-9]+$")]
    private static partial Regex AllowedPattern();
}
