namespace NexAccess.Application.Exceptions;

public abstract class AppException : Exception
{
    protected AppException(string code, string message) : base(message)
    {
        Code = code;
    }

    public string Code { get; }
}
