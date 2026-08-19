namespace Lending.Infrastructure;

public interface ICurrentUser
{
    string? UserId { get; }
    string? Name { get; }
}

public sealed class SystemCurrentUser : ICurrentUser
{
    public string? UserId => null;
    public string? Name => "system";
}
