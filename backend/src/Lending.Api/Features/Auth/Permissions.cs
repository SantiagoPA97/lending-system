namespace Lending.Api.Features.Auth;

public static class Permissions
{
    public const string Read = "portfolio.read";
    public const string ManagePortfolio = "portfolio.manage";
    public const string RecordRepayments = "repayments.record";
    public const string ReverseRepayments = "repayments.reverse";
    public const string CloseFacilities = "facilities.close";

    public static readonly IReadOnlyList<string> All =
        [Read, ManagePortfolio, RecordRepayments, ReverseRepayments, CloseFacilities];
}

public static class RolePermissions
{
    private static readonly Dictionary<string, string[]> Map = new(StringComparer.OrdinalIgnoreCase)
    {
        [AuthRoles.Viewer] = [Permissions.Read],
        [AuthRoles.Operator] = [Permissions.Read, Permissions.ManagePortfolio, Permissions.RecordRepayments],
        [AuthRoles.Admin] =
        [
            Permissions.Read,
            Permissions.ManagePortfolio,
            Permissions.RecordRepayments,
            Permissions.ReverseRepayments,
            Permissions.CloseFacilities
        ]
    };

    public static IReadOnlySet<string> For(IEnumerable<string> roles)
    {
        var granted = new HashSet<string>(StringComparer.Ordinal);
        foreach (var role in roles)
        {
            if (Map.TryGetValue(role, out var permissions))
                granted.UnionWith(permissions);
        }

        return granted;
    }
}
