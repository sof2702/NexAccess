using NexAccess.UnitTests.Support;

namespace NexAccess.UnitTests.Domain;

public class AccessPermissionTests
{
    [Theory]
    [InlineData("08:00", "17:00", "12:00", true)]
    [InlineData("08:00", "17:00", "07:59", false)]
    [InlineData("08:00", "17:00", "17:00", true)]
    [InlineData("22:00", "06:00", "23:30", true)]
    [InlineData("22:00", "06:00", "05:00", true)]
    [InlineData("22:00", "06:00", "12:00", false)]
    public void Time_window_supports_overnight_ranges(string start, string end, string time, bool expected)
    {
        var permission = TestData.AllDayPermission();
        permission.StartTime = TimeOnly.Parse(start);
        permission.EndTime = TimeOnly.Parse(end);

        Assert.Equal(expected, permission.AllowsTime(TimeOnly.Parse(time)));
    }

    [Fact]
    public void Missing_hours_allow_whole_day()
    {
        var permission = TestData.AllDayPermission();

        Assert.True(permission.AllowsTime(new TimeOnly(0, 0)));
        Assert.True(permission.AllowsTime(new TimeOnly(23, 59, 59)));
    }

    [Fact]
    public void Disabled_day_is_rejected()
    {
        var permission = TestData.AllDayPermission();
        permission.Sunday = false;

        Assert.False(permission.AllowsAt(new DateTime(2026, 10, 18, 10, 0, 0)));
        Assert.True(permission.AllowsAt(new DateTime(2026, 10, 19, 10, 0, 0)));
    }
}
