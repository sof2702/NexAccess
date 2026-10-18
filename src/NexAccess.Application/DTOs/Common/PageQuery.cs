using System.ComponentModel.DataAnnotations;

namespace NexAccess.Application.DTOs.Common;

public class PageQuery
{
    public const int MaxPageSize = 200;

    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, MaxPageSize)]
    public int PageSize { get; set; } = 50;

    public int Skip => (Page - 1) * PageSize;
}
