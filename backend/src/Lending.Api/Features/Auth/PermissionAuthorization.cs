using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace Lending.Api.Features.Auth;

public sealed class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}

// Permissions are resolved from the session's role claims on every request, so
// changes to the role→permission map take effect for existing sessions immediately.
public sealed class PermissionHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var roles = context.User.FindAll(ClaimTypes.Role).Select(c => c.Value);
        if (RolePermissions.For(roles).Contains(requirement.Permission))
            context.Succeed(requirement);
        return Task.CompletedTask;
    }
}
