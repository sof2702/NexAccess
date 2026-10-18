namespace NexAccess.Application.Exceptions;

public sealed class ConflictException : AppException
{
    public ConflictException(string code, string message) : base(code, message)
    {
    }
}
