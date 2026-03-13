using System.Linq.Expressions;
using B2BSpareParts.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace B2BSpareParts.Infrastructure.Services;

internal static class QueryableExtensions
{
    public static async Task<PageResponse<T>> ToPageAsync<T>(this IQueryable<T> query, PageRequest request, CancellationToken ct = default)
    {
        var totalCount = await query.CountAsync(ct);
        var items = await query.Skip((request.PageNumber - 1) * request.PageSize).Take(request.PageSize).ToListAsync(ct);

        return new PageResponse<T>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }

    public static IQueryable<T> ApplyCreatedAtSort<T>(this IQueryable<T> query, PageRequest request) where T : class
    {
        return request.SortDirection?.Equals("asc", StringComparison.OrdinalIgnoreCase) == true
            ? query.OrderBy(e => EF.Property<DateTimeOffset>(e, "CreatedAt"))
            : query.OrderByDescending(e => EF.Property<DateTimeOffset>(e, "CreatedAt"));
    }
}
