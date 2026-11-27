using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;

namespace PruebaTecnica.Infrastructure;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext,Exception exception,CancellationToken cancellationToken)
    {
        var (statusCode, title, detail) = exception switch
        {
            OracleException { Number: 20001 } => (StatusCodes.Status404NotFound,"Registro no encontrado","El registro solicitado no existe."),
            OracleException { Number: 20002 } => (StatusCodes.Status400BadRequest,"Página inválida","El número de página debe ser un entero mayor a cero."),
            OracleException { Number: 1 } => (StatusCodes.Status409Conflict,"Registro duplicado","Ya existe un registro con el mismo código y secuencia."),
            ArgumentException => (StatusCodes.Status400BadRequest,"Solicitud inválida","Los parámetros ingresados no son válidos."),
            KeyNotFoundException => (StatusCodes.Status404NotFound,"Registro no encontrado","El registro solicitado no existe."),
            _ => (StatusCodes.Status500InternalServerError,"Error interno del servidor","Ocurrió un error al procesar la solicitud.")
        };

        if (statusCode >= 500)
        {
            _logger.LogError(exception,"Error al procesar la solicitud {Path}",httpContext.Request.Path);
        }
        else
        {
            _logger.LogWarning("Solicitud rechazada. Estado: {StatusCode}. Ruta: {Path}",statusCode,httpContext.Request.Path);
        }

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path
        };

        problemDetails.Extensions["traceId"] =httpContext.TraceIdentifier;

        httpContext.Response.StatusCode = statusCode;

        await httpContext.Response.WriteAsJsonAsync(problemDetails,options: null,contentType: "application/problem+json",cancellationToken: cancellationToken);

        return true;
    }
}