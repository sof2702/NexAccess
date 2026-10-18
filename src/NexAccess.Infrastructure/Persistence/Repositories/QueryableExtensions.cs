using Microsoft.EntityFrameworkCore;
using NexAccess.Application.DTOs.Common;

namespace NexAccess.Infrastructure.Persistence.Repositories;

internal static class QueryableExtensions
{
    public static async Task<(IReadOnlyList<T> Items, int Total)> ToPageAsync<T>(
        this IQueryable<T> query,
        PageQuery page,
        CancellationToken cancellationToken)
    {
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip(page.Skip).Take(page.PageSize).ToListAsync(cancellationToken);
        return (items, total);
    }

    public static string ToLikePattern(this string term) =>
        "%" + term.Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
}
