namespace Lending.Api.Common;

public sealed class EntityNotFoundException(string entity, Guid id)
    : Exception($"{entity} with id '{id}' was not found.");
