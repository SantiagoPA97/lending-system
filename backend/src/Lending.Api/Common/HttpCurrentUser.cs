using System.Security.Claims;
using Lending.Infrastructure;

namespace Lending.Api.Common;

public sealed class HttpCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    public string? UserId => Principal?.FindFirstValue(ClaimTypes.NameIdentifier);

    public string? Name =>
        Principal?.Identity?.IsAuthenticated == true
            ? Principal.Identity.Name ?? UserId
            : "system";
}
