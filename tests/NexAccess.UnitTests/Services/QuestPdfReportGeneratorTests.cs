using NexAccess.Application.DTOs.Reports;
using NexAccess.Infrastructure.Reports;
using QuestPDF.Infrastructure;
using Xunit;
using Microsoft.Extensions.Options;
using NexAccess.Application.Options;
using NexAccess.Application.Services;

namespace NexAccess.UnitTests.Services;

public class QuestPdfReportGeneratorTests
{
    [Fact]
    public void Generates_valid_attendance_pdf()
    {
        // Licencia Community para el proyecto académico.
        QuestPDF.Settings.License = LicenseType.Community;

        var from = new DateTime(
            2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);

        var to = new DateTime(
            2026, 10, 26, 23, 59, 0, DateTimeKind.Utc);

        var request = new ReportRequest
        {
            Type = ReportType.Attendance,
            Format = ReportFormat.Pdf,
            From = from,
            To = to,
            IncludeDetails = true,
            IncludeSummary = true,
            GroupBy = new List<ReportGroupBy>
            {
                ReportGroupBy.Employee
            }
        };

        var report = new ReportResponse
        {
            Type = ReportType.Attendance,
            GeneratedAt = DateTime.UtcNow,
            From = from,
            To = to,
            TotalRecords = 2,

            Rows = new List<ReportRow>
            {
                new()
                {
                    Id = 1,
                    EmployeeId = 10,
                    EmployeeName = "Juan Pérez",
                    OccurredAt = from.AddHours(11),
                    Type = "ENTRADA",
                    Source = "DISPOSITIVO",
                    DeviceCode = "LECTOR-01"
                },
                new()
                {
                    Id = 2,
                    EmployeeId = 10,
                    EmployeeName = "Juan Pérez",
                    OccurredAt = from.AddHours(20),
                    Type = "SALIDA",
                    Source = "DISPOSITIVO",
                    DeviceCode = "LECTOR-01"
                }
            },

            Groups = new List<ReportGroupResult>
            {
                new()
                {
                    Keys = new Dictionary<string, string>
                    {
                        ["Employee"] = "10"
                    },
                    TotalRecords = 2,
                    Entries = 1,
                    Exits = 1
                }
            }
        };

        var localTime = new LocalTime(
    Options.Create(new AccessOptions
    {
        TimeZoneId = "America/Asuncion"
    }));

//var generator = new QuestPdfReportGenerator(localTime);

var currentUser = new MockCurrentUser();

var generator = new QuestPdfReportGenerator(
    localTime,
    currentUser);




        var pdf = generator.Generate(report, request);

        Assert.NotNull(pdf);
        Assert.True(pdf.Length > 1000);

        // Todos los PDF comienzan con la firma %PDF-
        var signature = System.Text.Encoding.ASCII.GetString(
            pdf, 0, 5);

        Assert.Equal("%PDF-", signature);
    }

private sealed class MockCurrentUser :
    NexAccess.Application.Interfaces.ICurrentUser
{
    public long? UserId => 1;
    public long? DeviceId => null;
    public string? IpAddress => "127.0.0.1";
    public Guid? SessionId { get; set; }
}
}

