namespace Backend.Core;

using Backend.Infrastructure.PostgreSQL;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyHeader()
                      .AllowAnyMethod();
            });
        });

        builder.Services.AddControllers();

        builder.Services.Configure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var errors = context.ModelState
                    .Where(e => e.Value != null && e.Value.Errors.Count > 0)
                    .SelectMany(kvp => kvp.Value!.Errors.Select(e =>
                    {
                        var errorMessage = !string.IsNullOrWhiteSpace(e.ErrorMessage) 
                            ? e.ErrorMessage 
                            : (e.Exception?.Message ?? "Erro de validação de modelo.");
                            
                        return new Backend.Core.Common.Results.ResultadoErro(
                            "VALIDATION_ERROR",
                            errorMessage,
                            string.IsNullOrEmpty(kvp.Key) ? null : kvp.Key
                        );
                    }))
                    .ToList();

                var result = Backend.Core.Common.Results.Resultado.Falha(errors);
                return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(result);
            };
        });

        builder.Services.AddOpenApiDocument();

        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("A Connection String 'DefaultConnection' não foi encontrada no appsettings.json.");

        builder.Services.AddPostgreSQLInfrastructure(connectionString);

        var app = builder.Build();

        app.UseMiddleware<Backend.Web.Middlewares.GlobalExceptionMiddleware>();

        app.UseCors();

        if (app.Environment.IsDevelopment())
        {
            app.UseOpenApi();

            app.UseSwaggerUi(config => {
                config.Path = string.Empty;
            });
        }

        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}