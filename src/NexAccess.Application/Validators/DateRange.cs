using NexAccess.Application.Exceptions;

namespace NexAccess.Application.Validators;

public static class DateRange
{
    public static void Ensure(DateTime? from, DateTime? to, int? maxDays = null)
    {
        if (from.HasValue && to.HasValue && from.Value > to.Value)
        {
            throw new AppValidationException("from", "La fecha inicial no puede ser posterior a la fecha final.");
        }

        if (maxDays.HasValue && from.HasValue && to.HasValue && (to.Value - from.Value).TotalDays > maxDays.Value)
        {
            throw new AppValidationException("to", $"El rango no puede superar {maxDays.Value} días.");
        }
    }

    public static DateTime? ToUtc(DateTime? value) => value.HasValue ? ToUtc(value.Value) : null;

    public static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };
}
