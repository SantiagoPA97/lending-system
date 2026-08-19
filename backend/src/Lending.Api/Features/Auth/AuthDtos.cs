namespace Lending.Api.Features.Auth;

public sealed record AuthUserResponse(
    bool Authenticated,
    string? Name,
    string? Email,
    IReadOnlyList<string> Roles,
    string Mode);

public sealed record AnonymousAuthResponse(bool Authenticated, string Mode);

public sealed record LogoutResponse(string? LogoutUrl);
