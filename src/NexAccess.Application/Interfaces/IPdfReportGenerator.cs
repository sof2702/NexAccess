
using NexAccess.Application.DTOs.Reports;

namespace NexAccess.Application.Interfaces;

public interface IPdfReportGenerator
{
    byte[] Generate(
        ReportResponse report,
        ReportRequest request);
}

