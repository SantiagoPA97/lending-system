using Microsoft.AspNetCore.Mvc;

namespace Lending.Api.Common;

// Session cookies are sent ambiently by the browser, so mutations must also carry
// a custom header that cross-site forms cannot set (SameSite=Lax is the first layer).
public sealed class CsrfProtectionMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Requested-With";

    public async Task InvokeAsync(HttpContext context)
    {
        var request = context.Request;
        var safeMethod = HttpMethods.IsGet(request.Method)
            || HttpMethods.IsHead(request.Method)
            || HttpMethods.IsOptions(request.Method);

        if (!safeMethod
            && request.Path.StartsWithSegments("/api")
            && !request.Headers.ContainsKey(HeaderName))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(
                new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Missing CSRF header",
                    Detail = $"Mutating requests to /api must include the {HeaderName} header.",
                    Instance = request.Path,
                    Extensions = { ["errorCode"] = "csrf.missing_header" }
                },
                options: null,
                contentType: "application/problem+json");
            return;
        }

        await next(context);
    }
}
