namespace NexAccess.Application.Exceptions;

public sealed class PersistenceException : AppException
{
    public PersistenceException(string message, Exception innerException)
        : base("PERSISTENCE_ERROR", message)
    {
        InnerPersistenceError = innerException;
    }

    public Exception InnerPersistenceError { get; }
}
