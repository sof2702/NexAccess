namespace NexAccess.Application.Exceptions;

public sealed class BusinessRuleException : AppException
{
    public BusinessRuleException(string code, string message) : base(code, message)
    {
    }
}
