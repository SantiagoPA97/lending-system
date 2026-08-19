using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Lending.Api.Features.Auth;

public static class AuthEndpoints
{
    public static RouteGroupBuilder MapAuthEndpoints(this RouteGroupBuilder group, string mode)
    {
        var isAuth0 = mode == AuthSetup.Auth0Mode;

        group.MapGet("/login", (string? returnUrl) => isAuth0
            ? Results.Challenge(
                new AuthenticationProperties { RedirectUri = SafeLocalUrl(returnUrl) },
                [AuthSetup.Auth0Scheme])
            : Results.NotFound());

        group.MapGet("/dev-login", async (HttpContext context, string? role) =>
        {
            if (isAuth0)
                return Results.NotFound();

            var normalized = role?.Trim().ToLowerInvariant() ?? string.Empty;
            if (!AuthRoles.All.Contains(normalized))
                return Results.Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid role",
                    detail: $"role must be one of: {string.Join(", ", AuthRoles.All)}.");

            var display = char.ToUpperInvariant(normalized[0]) + normalized[1..];
            var identity = new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, $"dev|{normalized}"),
                    new Claim(ClaimTypes.Name, $"Demo {display}"),
                    new Claim(ClaimTypes.Email, $"demo.{normalized}@ledgerline.local"),
                    new Claim(ClaimTypes.Role, normalized)
                ],
                CookieAuthenticationDefaults.AuthenticationScheme);

            await context.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                new AuthenticationProperties { IsPersistent = true });

            return Results.Redirect("/");
        });

        group.MapPost("/logout", async (HttpContext context, IConfiguration configuration) =>
        {
            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            // Auth0 federated sign-out is a browser navigation, not a fetch, so the
            // SPA receives the URL and redirects itself after the cookie is cleared.
            var logoutUrl = isAuth0 ? AuthSetup.BuildAuth0LogoutUrl(configuration, context.Request) : null;
            return Results.Ok(new LogoutResponse(logoutUrl));
        });

        group.MapGet("/me", (ClaimsPrincipal user) =>
        {
            if (user.Identity?.IsAuthenticated != true)
                return Results.Ok<object>(new AnonymousAuthResponse(false, mode));

            var roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value)
                .Concat(user.FindAll(AuthSetup.Auth0RoleClaim).Select(c => c.Value))
                .Select(r => r.ToLowerInvariant())
                .Distinct()
                .ToList();

            return Results.Ok<object>(new AuthUserResponse(
                true,
                user.Identity.Name,
                user.FindFirstValue(ClaimTypes.Email) ?? user.FindFirstValue("email"),
                roles,
                mode));
        });

        return group;
    }

    private static string SafeLocalUrl(string? url) =>
        !string.IsNullOrEmpty(url) && url.StartsWith('/') && !url.StartsWith("//") && !url.StartsWith("/\\")
            ? url
            : "/";
}
