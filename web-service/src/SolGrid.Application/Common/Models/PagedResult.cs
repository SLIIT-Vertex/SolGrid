/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: PagedResult.cs
 * Description: Represents one page of results shared by all paged application queries.
 * Contributor: Kavishi Godage
 */

namespace SolGrid.Application.Common.Models;

public sealed class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();

    public long TotalCount { get; init; }

    public int PageNumber { get; init; }

    public int PageSize { get; init; }
}
