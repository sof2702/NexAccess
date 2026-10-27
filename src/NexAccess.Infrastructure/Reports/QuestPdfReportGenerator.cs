using System.Globalization;
using NexAccess.Application.DTOs.Reports;
using NexAccess.Application.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using NexAccess.Application.Services;

namespace NexAccess.Infrastructure.Reports;

public sealed class QuestPdfReportGenerator : IPdfReportGenerator
{
    private const string Primary = "#194E4A";
    private const string Muted = "#64748B";
    private readonly LocalTime _localTime;
    private readonly ICurrentUser _currentUser;

    public QuestPdfReportGenerator(
        LocalTime localTime,
        ICurrentUser currentUser)
    {
        _localTime = localTime;
        _currentUser = currentUser;
    }

    
    public byte[] Generate(ReportResponse report, ReportRequest request)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(request);

var defaultColumns = report.Type == ReportType.Attendance
    ? new List<string>
    {
        "occurredAt",
        "employeeName",
        "type",
        "source",
        "deviceCode"
    }
    : new List<string>
    {
        "occurredAt",
        "employeeName",
        "result",
        "areaName",
        "deviceCode"
    };

var selectedColumns = request.Columns.Count > 0
    ? request.Columns
    : defaultColumns;

var landscape = request.IncludeDetails &&
                selectedColumns.Count > 5;



        var title = report.Type switch
        {
            ReportType.Attendance => "Reporte de asistencia",
            ReportType.AccessEvents => "Reporte de accesos físicos",
            _ => "Reporte NexAccess"
        };

        return Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(landscape ? PageSizes.A4.Landscape() : PageSizes.A4);
                page.Margin(35);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(text => text.FontSize(9));

                page.Header().Column(header =>
                {
                    header.Item().Row(row =>
                    {
                        row.RelativeItem().Column(left =>
                        {
                            left.Item().Text("NEXACCESS")
                                .Bold()
                                .FontSize(23)
                                .FontColor(Primary);

                            left.Item().Text("CONTROL DE ACCESO Y ASISTENCIA")
                                .FontSize(8)
                                .FontColor(Muted);
                        });

                        row.ConstantItem(130)
                            .AlignRight()
                            .Text("REPORTE INSTITUCIONAL")
                            .FontSize(8)
                            .FontColor(Muted);
                    });

                    header.Item()
                        .PaddingTop(10)
                        .LineHorizontal(2)
                        .LineColor(Primary);
                });

                page.Content().PaddingVertical(15).Column(column =>
                {
                    column.Spacing(12);

                    column.Item().AlignCenter()
                        .Text(title.ToUpperInvariant())
                        .Bold()
                        .FontSize(15)
                        .FontColor(Primary);

                    column.Item().Column(info =>
                    {
                        info.Spacing(4);

                        info.Item().Text(
                            $"Desde: {LocalDate(report.From)}");

                        info.Item().Text(
                            $"Hasta: {LocalDate(report.To)}");

                        info.Item().Text(
                            $"Generado: {LocalDate(report.GeneratedAt)} (hora de Paraguay)");

                        info.Item().Text(
                            $"Total de registros: {report.TotalRecords:N0}");

			info.Item().Text(
    $"Generado por: Usuario #{_currentUser.UserId?.ToString() ?? "No identificado"}");

info.Item().Text(
    $"Zona horaria: {_localTime.Zone.Id}");
                    });



		    column.Item().Column(filters =>
{
    filters.Spacing(4);

    filters.Item()
        .Text("FILTROS APLICADOS")
        .Bold()
        .FontSize(11)
        .FontColor(Primary);

    filters.Item().Text(
        $"Empleados: {FormatIds(request.EmployeeIds)}");

    if (report.Type == ReportType.AccessEvents)
    {
        filters.Item().Text(
            $"Áreas: {FormatIds(request.AreaIds)}");

        filters.Item().Text(
            $"Resultados: {FormatValues(request.AccessResults)}");
    }
    else
    {
        filters.Item().Text(
            $"Tipos de marcación: {FormatValues(request.AttendanceTypes)}");

        filters.Item().Text(
            $"Origen: {FormatValues(request.AttendanceSources)}");
    }

    filters.Item().Text(
        $"Dispositivos: {FormatIds(request.DeviceIds)}");

    filters.Item().Text(
        $"Agrupaciones: {FormatValues(request.GroupBy)}");
});


                    if (request.IncludeSummary && report.Groups.Count > 0)
                    {
                        column.Item().PaddingTop(8)
                            .Text("RESUMEN AGRUPADO")
                            .Bold()
                            .FontSize(11)
                            .FontColor(Primary);

                        foreach (var group in report.Groups)
                        {
                            var keys = string.Join(" / ",
                                group.Keys.Select(k =>
                                    $"{k.Key}: {k.Value}"));

                            column.Item()
                                .BorderBottom(1)
                                .BorderColor(Colors.Grey.Lighten2)
                                .PaddingVertical(5)
                                .Column(item =>
                                {
                                    item.Item().Text(keys).Bold();

                                    item.Item().Text(
                                        $"Total: {group.TotalRecords} | " +
                                        $"Entradas: {group.Entries} | " +
                                        $"Salidas: {group.Exits} | " +
                                        $"Autorizados: {group.Authorized} | " +
                                        $"Rechazados: {group.Rejected}");
                                });
                        }
                    }

                    if (request.IncludeDetails)
                    {
                        column.Item().PaddingTop(8)
                            .Text("DETALLE DE REGISTROS")
                            .Bold()
                            .FontSize(11)
                            .FontColor(Primary);

                        

column.Item().Table(table =>
{
    table.ColumnsDefinition(columns =>
    {
        foreach (var selected in selectedColumns)
        {
            columns.RelativeColumn(
                selected.Equals("employeeName",
                    StringComparison.OrdinalIgnoreCase) ? 2 : 1);
        }
    });

    table.Header(header =>
    {
        foreach (var selected in selectedColumns)
        {
            CellHeader(
                header.Cell(),
                GetColumnTitle(selected));
        }
    });

    foreach (var record in report.Rows)
    {
        foreach (var selected in selectedColumns)
        {
            Cell(
                table.Cell(),
                GetColumnValue(record, selected));
        }
    }
});

                    }
                });

                page.Footer()
                    .BorderTop(1)
                    .BorderColor(Colors.Grey.Lighten2)
                    .PaddingTop(7)
                    .Row(row =>
                    {
                        row.RelativeItem()
                            .Text("NexAccess · Documento generado electrónicamente")
                            .FontSize(8)
                            .FontColor(Muted);

                        row.ConstantItem(95)
                            .AlignRight()
                            .Text(text =>
                            {
                                text.Span("Página ");
                                text.CurrentPageNumber();
                                text.Span(" de ");
                                text.TotalPages();
                            });
                    });
            });
        }).GeneratePdf();
    }

    private string LocalDate(DateTime date) =>
    _localTime.ToLocal(date).ToString(
        "dd/MM/yyyy HH:mm:ss",
        CultureInfo.InvariantCulture);

    private static void CellHeader(
        IContainer container,
        string value)
    {
        container
            .Background(Primary)
            .Padding(5)
            .Text(value)
            .Bold()
            .FontColor(Colors.White)
            .FontSize(8);
    }

    private static void Cell(
        IContainer container,
        string value)
    {
        container
            .BorderBottom(1)
            .BorderColor(Colors.Grey.Lighten2)
            .Padding(5)
            .Text(value)
            .FontSize(8);
    }

	
private static string GetColumnTitle(string column) =>
    column.ToLowerInvariant() switch
    {
        "occurredat" => "Fecha y hora",
        "employeeid" => "ID empleado",
        "employeename" => "Empleado",
        "type" => "Tipo",
        "source" => "Origen",
        "result" => "Resultado",
        "reason" => "Motivo",
        "areaname" => "Área",
        "devicecode" => "Dispositivo",
        "note" => "Observación",
        _ => column
    };

private string GetColumnValue(
    ReportRow row,
    string column) =>
    column.ToLowerInvariant() switch
    {
        "occurredat" => LocalDate(row.OccurredAt),
        "employeeid" => row.EmployeeId?.ToString() ?? "—",
        "employeename" => row.EmployeeName ?? "Sin identificar",
        "type" => row.Type ?? "—",
        "source" => row.Source ?? "—",
        "result" => row.Result ?? "—",
        "reason" => row.Reason ?? "—",
        "areaname" => row.AreaName ?? "—",
        "devicecode" => row.DeviceCode ?? "—",
        "note" => row.Note ?? "—",
        _ => "—"
    };




private static string FormatIds(IEnumerable<long> ids)
{
    var values = ids.ToArray();

    return values.Length == 0
        ? "Todos"
        : string.Join(", ", values);
}

private static string FormatValues<T>(IEnumerable<T> values)
{
    var items = values.Select(x => x?.ToString()).ToArray();

    return items.Length == 0
        ? "Todos"
        : string.Join(", ", items);
}

}

