using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace Lending.Api.Features.Auth;

public static class AuthRoles
{
    public const string Viewer = "viewer";
    public const string Operator = "operator";
    public const string Admin = "admin";
    public static readonly string[] All = [Viewer, Operator, Admin];
}

public static class AuthPolicies
{
    public const string Viewer = "Viewer";
    public const string Operator = "Operator";
    public const string Admin = "Admin";
}

public static class AuthSetup
{
    public const string BypassMode = "Bypass";
    public const string Auth0Mode = "Auth0";
    public const string Auth0Scheme = "Auth0";
    public const string Auth0RoleClaim = "https://lending/roles";

    public static string ResolveMode(IConfiguration configuration, IHostEnvironment environment)
    {
        var mode = configuration["Auth:Mode"];
        if (string.IsNullOrWhiteSpace(mode) || mode.Equals(BypassMode, StringComparison.OrdinalIgnoreCase))
        {
            // Bypass hands out role sessions to anyone via /auth/dev-login, so a
            // misconfigured production deploy must fail at startup, not run open.
            if (environment.IsProduction() && !configuration.GetValue<bool>("Auth:AllowBypassInProduction"))
                throw new InvalidOperationException(
                    "Auth:Mode resolved to 'Bypass' in the Production environment. Bypass exposes " +
                    "/auth/dev-login, which grants admin sessions to anonymous users. Set Auth__Mode=Auth0 " +
                    "(with the Auth:Auth0 section configured), or explicitly opt in with " +
                    "Auth__AllowBypassInProduction=true if this deployment is intentionally open (e.g. a demo).");
            return BypassMode;
        }

        if (mode.Equals(Auth0Mode, StringComparison.OrdinalIgnoreCase))
            return Auth0Mode;
        throw new InvalidOperationException(
            $"Unsupported Auth:Mode '{mode}'. Use '{BypassMode}' or '{Auth0Mode}'.");
    }

    public static void AddLendingAuth(this WebApplicationBuilder builder, string mode)
    {
        var auth = builder.Services
            .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(ConfigureCookie);

        if (mode == Auth0Mode)
            auth.AddOpenIdConnect(Auth0Scheme, options => ConfigureAuth0(options, builder.Configuration));

        // Role hierarchy admin ⊃ operator ⊃ viewer, expressed as widening role sets.
        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy(AuthPolicies.Viewer,
                p => p.RequireRole(AuthRoles.Viewer, AuthRoles.Operator, AuthRoles.Admin));
            options.AddPolicy(AuthPolicies.Operator,
                p => p.RequireRole(AuthRoles.Operator, AuthRoles.Admin));
            options.AddPolicy(AuthPolicies.Admin,
                p => p.RequireRole(AuthRoles.Admin));
        });
    }

    private static void ConfigureCookie(CookieAuthenticationOptions options)
    {
        options.Cookie.Name = "ledgerline.session";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        // The SPA consumes JSON, so unauthenticated/forbidden must be status codes,
        // never 302 redirects to a login page.
        options.Events.OnRedirectToLogin = context => WriteProblem(
            context.HttpContext,
            StatusCodes.Status401Unauthorized,
            "Unauthorized",
            "Authentication is required for this resource.");
        options.Events.OnRedirectToAccessDenied = context => WriteProblem(
            context.HttpContext,
            StatusCodes.Status403Forbidden,
            "Forbidden",
            "Your role does not permit this action.");
    }

    private static void ConfigureAuth0(OpenIdConnectOptions options, IConfiguration configuration)
    {
        var domain = Require(configuration, "Auth:Auth0:Domain");
        options.Authority = $"https://{domain}";
        options.ClientId = Require(configuration, "Auth:Auth0:ClientId");
        options.ClientSecret = Require(configuration, "Auth:Auth0:ClientSecret");
        options.ResponseType = "code";
        options.CallbackPath = "/auth/callback";
        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("email");
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            NameClaimType = "name",
            RoleClaimType = ClaimTypes.Role
        };
        options.Events = new OpenIdConnectEvents
        {
            // Auth0 delivers roles in a namespaced custom claim (added by an Auth0
            // Action); copy them onto standard claims so policies and ICurrentUser
            // work identically in both modes.
            OnTokenValidated = context =>
            {
                if (context.Principal?.Identity is ClaimsIdentity identity)
                {
                    foreach (var role in context.Principal.FindAll(Auth0RoleClaim).ToList())
                        identity.AddClaim(new Claim(ClaimTypes.Role, role.Value.ToLowerInvariant()));
                    AddIfMissing(identity, ClaimTypes.NameIdentifier, context.Principal.FindFirstValue("sub"));
                    AddIfMissing(identity, ClaimTypes.Email, context.Principal.FindFirstValue("email"));
                }
                return Task.CompletedTask;
            }
        };
    }

    public static string BuildAuth0LogoutUrl(IConfiguration configuration, HttpRequest request)
    {
        var domain = Require(configuration, "Auth:Auth0:Domain");
        var clientId = Require(configuration, "Auth:Auth0:ClientId");
        var returnTo = Uri.EscapeDataString($"{request.Scheme}://{request.Host}/");
        return $"https://{domain}/v2/logout?client_id={Uri.EscapeDataString(clientId)}&returnTo={returnTo}";
    }

    private static void AddIfMissing(ClaimsIdentity identity, string type, string? value)
    {
        if (!string.IsNullOrEmpty(value) && !identity.HasClaim(c => c.Type == type))
            identity.AddClaim(new Claim(type, value));
    }

    private static string Require(IConfiguration configuration, string key) =>
        configuration[key] is { Length: > 0 } value
            ? value
            : throw new InvalidOperationException(
                $"'{key}' must be configured when Auth:Mode={Auth0Mode} " +
                $"(env override: {key.Replace(":", "__")}).");

    private static Task WriteProblem(HttpContext context, int status, string title, string detail)
    {
        context.Response.StatusCode = status;
        return context.Response.WriteAsJsonAsync(
            new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = detail,
                Instance = context.Request.Path
            },
            options: null,
            contentType: "application/problem+json");
    }
}
