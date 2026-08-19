namespace Lending.Api.Common;

public static class Pagination
{
    public const int MaxPageSize = 100;

    /// <summary>Clamps paging query values into valid bounds (page >= 1, 1 &lt;= pageSize &lt;= 100).</summary>
    public static (int Page, int PageSize) Normalize(int page, int pageSize) =>
        (Math.Max(page, 1), Math.Clamp(pageSize, 1, MaxPageSize));
}
