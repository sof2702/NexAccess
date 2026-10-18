namespace NexAccess.Application.Exceptions;

public sealed class UnauthorizedException : AppException
{
    public UnauthorizedException(string code, string message) : base(code, message)
    {
    }
}
