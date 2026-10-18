namespace NexAccess.Domain.Entities;

public class AccessPermission
{
    public long Id { get; set; }
    public long RoleId { get; set; }
    public long AreaId { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public bool Monday { get; set; } = true;
    public bool Tuesday { get; set; } = true;
    public bool Wednesday { get; set; } = true;
    public bool Thursday { get; set; } = true;
    public bool Friday { get; set; } = true;
    public bool Saturday { get; set; }
    public bool Sunday { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }

    public Role? Role { get; set; }
    public Area? Area { get; set; }

    public bool AllowsDay(DayOfWeek day) => day switch
    {
        DayOfWeek.Monday => Monday,
        DayOfWeek.Tuesday => Tuesday,
        DayOfWeek.Wednesday => Wednesday,
        DayOfWeek.Thursday => Thursday,
        DayOfWeek.Friday => Friday,
        DayOfWeek.Saturday => Saturday,
        _ => Sunday
    };

    public bool AllowsTime(TimeOnly time)
    {
        var start = StartTime ?? TimeOnly.MinValue;
        var end = EndTime ?? TimeOnly.MaxValue;
        return start <= end
            ? time >= start && time <= end
            : time >= start || time <= end;
    }

    public bool AllowsAt(DateTime localDateTime) =>
        AllowsDay(localDateTime.DayOfWeek) && AllowsTime(TimeOnly.FromDateTime(localDateTime));
}
