namespace Lending.Api.Common;

/// <summary>
/// API-layer error codes surfaced to clients via the RFC7807 "errorCode" extension.
/// The string values are part of the API contract and must not change.
/// Domain-level codes live in <see cref="Lending.Domain.DomainErrors"/>.
/// </summary>
public static class ApiErrors
{
    public static class Csrf
    {
        public const string MissingHeader = "csrf.missing_header";
    }

    public static class Assistant
    {
        public const string UpstreamError = "assistant.upstream_error";
    }
}
