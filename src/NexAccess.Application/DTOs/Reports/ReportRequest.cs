using NexAccess.Domain.Enums;

namespace NexAccess.Application.DTOs.Reports;

public sealed class ReportRequest
{
    // Tipo y formato del reporte
    public ReportType Type { get; set; } = ReportType.Attendance;
    public ReportFormat Format { get; set; } = ReportFormat.Json;

    // Rango de fechas obligatorio
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }

    // Filtros combinables
    public List<long> EmployeeIds { get; set; } = new();
    public List<long> AreaIds { get; set; } = new();
    public List<long> DeviceIds { get; set; } = new();

    // Filtros de asistencia
    public List<AttendanceType> AttendanceTypes { get; set; } = new();
    public List<AttendanceSource> AttendanceSources { get; set; } = new();

    // Filtros de accesos
    public List<AccessResult> AccessResults { get; set; } = new();

    // Agrupaciones múltiples
    public List<ReportGroupBy> GroupBy { get; set; } = new();

    // Columnas seleccionables
    public List<string> Columns { get; set; } = new();

    // Opciones de presentación
    public bool IncludeSummary { get; set; } = true;
    public bool IncludeDetails { get; set; } = true;
}
