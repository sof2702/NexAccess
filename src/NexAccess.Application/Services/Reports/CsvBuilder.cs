using System.Globalization;
using System.Text;

namespace NexAccess.Application.Services.Reports;

public static class CsvBuilder
{
    private static readonly char[] FormulaPrefixes = { '=', '+', '-', '@', '\t', '\r' };

    public static string Build(IEnumerable<string> headers, IEnumerable<IEnumerable<object?>> rows)
    {
        var builder = new StringBuilder();
        AppendRow(builder, headers);
        foreach (var row in rows)
        {
            AppendRow(builder, row);
        }

        return builder.ToString();
    }

    private static void AppendRow(StringBuilder builder, IEnumerable<object?> values)
    {
        builder.AppendJoin(',', values.Select(Escape));
        builder.Append("\r\n");
    }

    private static string Escape(object? value)
    {
        var text = value switch
        {
            null => string.Empty,
            DateTime date => date.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            DateOnly date => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty
        };

        if (text.Length > 0 && FormulaPrefixes.Contains(text[0]) && value is string)
        {
            text = "'" + text;
        }

        return text.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0
            ? "\"" + text.Replace("\"", "\"\"") + "\""
            : text;
    }
}
