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

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        try
        {
            _logger.LogError(exception, "Ocorreu uma exceção: {Message}", exception.Message);

            if (context.Response.HasStarted)
            {
                _logger.LogWarning("A resposta já foi iniciada, a exceção não pôde ser formatada pelo middleware.");
                return;
            }

            context.Response.Clear();
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
                    await context.Response.WriteAsync(JsonSerializer.Serialize(resultadoDuplicidade, options));
                    return;
                }
            }

            // Para outras exceções, retorna 500 formatado como Result Pattern
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            var resultadoGenerico = Resultado.Falha(
                new ResultadoErro("ERRO_INTERNO", "Ocorreu um erro inesperado no servidor.")
            );
            var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            await context.Response.WriteAsync(JsonSerializer.Serialize(resultadoGenerico, jsonOptions));
        }
        catch (Exception ex)
        {
            // Fallback absoluto para evitar vazar a stack trace original
            _logger.LogCritical(ex, "Falha crítica no GlobalExceptionMiddleware ao processar outra exceção.");
            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync("{\"success\":false,\"errors\":[{\"code\":\"ERRO_CRITICO\",\"message\":\"Erro crítico interno.\"}]}");
            }
        }
    }
}
