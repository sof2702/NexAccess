namespace NexAccess.Application.Exceptions;

public sealed class NotFoundException : AppException
{
    public NotFoundException(string entity, object id)
        : base("NOT_FOUND", $"{entity} con identificador '{id}' no existe.")
    {
    }
}
