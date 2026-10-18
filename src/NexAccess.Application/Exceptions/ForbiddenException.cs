namespace NexAccess.Application.Exceptions;

public sealed class ForbiddenException : AppException
{
    public ForbiddenException(string code, string message) : base(code, message)
    {
    }
}
