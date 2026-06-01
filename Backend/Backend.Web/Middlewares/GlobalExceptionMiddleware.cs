using System.Net;
using System.Text.Json;
using Backend.Core.Common;

namespace Backend.Web.Middlewares;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
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
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        _logger.LogError(exception, "Ocorreu uma exceção: {Message}", exception.Message);

        context.Response.ContentType = "application/json";

        var isPostgresException = exception.GetType().Name == "PostgresException";
        
        if (isPostgresException)
        {
            var sqlStateProperty = exception.GetType().GetProperty("SqlState");
            var sqlState = sqlStateProperty?.GetValue(exception)?.ToString();

            // 23505 = Unique Constraint Violation
            if (sqlState == "23505")
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                
                var resultadoDuplicidade = Resultado.Falha(
                    new ResultadoErro("DUPLICIDADE", "Este registro já existe (dados duplicados).")
                );

                var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                return context.Response.WriteAsync(JsonSerializer.Serialize(resultadoDuplicidade, options));
            }
        }

        // Para outras exceções, retorna 500 formatado como Result Pattern
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        var resultadoGenerico = Resultado.Falha(
            new ResultadoErro("ERRO_INTERNO", "Ocorreu um erro inesperado no servidor.")
        );
        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        return context.Response.WriteAsync(JsonSerializer.Serialize(resultadoGenerico, jsonOptions));
    }
}
