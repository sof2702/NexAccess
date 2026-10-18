namespace NexAccess.Application.Exceptions;

public sealed class AppValidationException : AppException
{
    public AppValidationException(IReadOnlyDictionary<string, string[]> errors)
        : base("VALIDATION_ERROR", "Los datos enviados no son válidos.")
    {
        Errors = errors;
    }

    public AppValidationException(string field, string message)
        : this(new Dictionary<string, string[]> { [field] = new[] { message } })
    {
    }

    public IReadOnlyDictionary<string, string[]> Errors { get; }
}
