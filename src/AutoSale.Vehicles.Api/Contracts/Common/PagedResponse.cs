using AutoSale.Application.Common;

namespace AutoSale.Api.Contracts.Common;

public sealed record PagedResponse<TItem>(IReadOnlyCollection<TItem> Items, int Page, int PageSize, int TotalCount)
{
    public static PagedResponse<TItem> From<TSource>(PagedResult<TSource> page, Func<TSource, TItem> mapper) =>
        new(page.Items.Select(mapper).ToArray(), page.Page, page.PageSize, page.TotalCount);
}
