using NexAccess.Application.Exceptions;
using Npgsql;

namespace NexAccess.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            _logger.LogDebug("Solicitud cancelada por el cliente: {Path}", context.Request.Path);
        }
        catch (AppValidationException exception)
        {
            await ProblemResponses.WriteAsync(context, StatusCodes.Status400BadRequest, exception.Code, exception.Message, exception.Errors.ToDictionary(item => item.Key, item => item.Value));
        }
        catch (PersistenceException exception)
        {
            _logger.LogError(exception.InnerPersistenceError, "Error de persistencia en {Path}", context.Request.Path);
            await ProblemResponses.WriteAsync(context, StatusCodes.Status500InternalServerError, exception.Code, exception.Message);
        }
        catch (AppException exception)
        {
            await ProblemResponses.WriteAsync(context, StatusFor(exception), exception.Code, exception.Message);
        }
        catch (NpgsqlException exception)
        {
            _logger.LogError(exception, "Error de conexión con PostgreSQL en {Path}", context.Request.Path);
            await ProblemResponses.WriteAsync(context, StatusCodes.Status503ServiceUnavailable, "DATABASE_UNAVAILABLE", "La base de datos no está disponible.");
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error no controlado en {Path}", context.Request.Path);
            await ProblemResponses.WriteAsync(context, StatusCodes.Status500InternalServerError, "INTERNAL_ERROR", "Ocurrió un error inesperado.");
        }
    }

    private static int StatusFor(AppException exception) => exception switch
    {
        NotFoundException => StatusCodes.Status404NotFound,
        ConflictException => StatusCodes.Status409Conflict,
        BusinessRuleException => StatusCodes.Status422UnprocessableEntity,
        ForbiddenException => StatusCodes.Status403Forbidden,
        UnauthorizedException => StatusCodes.Status401Unauthorized,
        _ => StatusCodes.Status400BadRequest
    };
}
