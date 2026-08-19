using Microsoft.Extensions.Primitives;
using Serilog.Context;

namespace Lending.Api.Common;

public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId =
            context.Request.Headers.TryGetValue(HeaderName, out var incoming) && !StringValues.IsNullOrEmpty(incoming)
                ? incoming.ToString()
                : Guid.NewGuid().ToString("n");

        context.Items[HeaderName] = correlationId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await next(context);
        }
    }
}
