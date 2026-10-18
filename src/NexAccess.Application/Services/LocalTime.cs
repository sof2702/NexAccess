using Microsoft.Extensions.Options;
using NexAccess.Application.Options;

namespace NexAccess.Application.Services;

public class LocalTime
{
    public LocalTime(IOptions<AccessOptions> options)
    {
        Zone = Resolve(options.Value.TimeZoneId);
    }

    public TimeZoneInfo Zone { get; }

    public DateTime ToLocal(DateTime utc) => TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Zone);

    public DateOnly LocalDate(DateTime utc) => DateOnly.FromDateTime(ToLocal(utc));

    private static TimeZoneInfo Resolve(string? timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            return TimeZoneInfo.Utc;
        }

        return TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId, out var zone)
            ? zone
            : throw new InvalidOperationException($"La zona horaria '{timeZoneId}' configurada en Access:TimeZoneId no existe.");
    }
}
