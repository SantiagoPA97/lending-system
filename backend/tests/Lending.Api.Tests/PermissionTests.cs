using System.Net;
using System.Text.Json.Nodes;
using Lending.Api.Features.Auth;

namespace Lending.Api.Tests;

public class RolePermissionsTests
{
    [Fact]
    public void For_UnknownRole_GrantsNothing()
    {
        Assert.Empty(RolePermissions.For(["auditor", "superuser"]));
    }

    [Fact]
    public void For_EmptyRoles_GrantsNothing()
    {
        Assert.Empty(RolePermissions.For([]));
    }

    [Fact]
    public void EveryPermission_IsGrantedToAtLeastOneRole()
    {
        var allGranted = RolePermissions.For(AuthRoles.All);
        foreach (var permission in Permissions.All)
            Assert.Contains(permission, allGranted);
    }

    [Fact]
    public void For_IsCaseInsensitiveOnRoleNames()
    {
        Assert.Equal(RolePermissions.For(["admin"]), RolePermissions.For(["Admin"]));
    }

    [Fact]
    public void For_UnionsAcrossRoles_IgnoringUnknownOnes()
    {
        var granted = RolePermissions.For(["viewer", "operator", "ghost"]);
        Assert.True(granted.SetEquals(
            [Permissions.Read, Permissions.ManagePortfolio, Permissions.RecordRepayments]));
    }
}

[Collection(ApiCollection.Name)]
public class AuthMePermissionsTests(PostgresFixture fixture)
{
    [Theory]
    [InlineData("viewer", new[] { "portfolio.read" })]
    [InlineData("operator", new[] { "portfolio.manage", "portfolio.read", "repayments.record" })]
    [InlineData("admin", new[]
    {
        "facilities.close", "portfolio.manage", "portfolio.read", "repayments.record", "repayments.reverse"
    })]
    public async Task AuthMe_ReturnsSortedPermissionsForRole(string role, string[] expected)
    {
        var client = await fixture.CreateClientAsync(role);

        var response = await client.GetAsync("/auth/me");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = JsonNode.Parse(await response.Content.ReadAsStringAsync())!;
        Assert.True(body["authenticated"]!.GetValue<bool>());
        Assert.Equal(role, Assert.Single(body["roles"]!.AsArray())!.GetValue<string>());
        Assert.Equal(expected, body["permissions"]!.AsArray().Select(n => n!.GetValue<string>()).ToArray());
    }
}
