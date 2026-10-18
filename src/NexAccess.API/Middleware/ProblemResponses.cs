using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace NexAccess.API.Middleware;

public static class ProblemResponses
{
    public static ProblemDetails Create(HttpContext context, int status, string code, string detail, IDictionary<string, string[]>? errors = null)
    {
        var problem = new ProblemDetails
        {
            Status = status,
            Title = TitleFor(status),
            Detail = detail,
            Instance = context.Request.Path
        };
        problem.Extensions["code"] = code;
        problem.Extensions["traceId"] = context.TraceIdentifier;
        if (errors is not null)
        {
            problem.Extensions["errors"] = errors;
        }

        return problem;
    }

    public static async Task WriteAsync(HttpContext context, int status, string code, string detail, IDictionary<string, string[]>? errors = null)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(Create(context, status, code, detail, errors), (System.Text.Json.JsonSerializerOptions?)null, "application/problem+json");
    }

    public static IActionResult FromModelState(HttpContext context, ModelStateDictionary modelState)
    {
        var errors = modelState
            .Where(entry => entry.Value is { Errors.Count: > 0 })
            .ToDictionary(
                entry => entry.Key,
                entry => entry.Value!.Errors.Select(error => string.IsNullOrEmpty(error.ErrorMessage) ? "Valor inválido." : error.ErrorMessage).ToArray());
        var problem = Create(context, StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "Los datos enviados no son válidos.", errors);
        return new ObjectResult(problem) { StatusCode = StatusCodes.Status400BadRequest, ContentTypes = { "application/problem+json" } };
    }

    private static string TitleFor(int status) => status switch
    {
        400 => "Solicitud inválida",
        401 => "No autenticado",
        403 => "Acceso prohibido",
        404 => "No encontrado",
        409 => "Conflicto",
        422 => "Regla de negocio no cumplida",
        429 => "Demasiadas solicitudes",
        503 => "Servicio no disponible",
        _ => "Error interno"
    };
}
