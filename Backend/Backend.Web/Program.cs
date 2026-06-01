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