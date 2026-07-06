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

        builder.Services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
                options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
                options.JsonSerializerOptions.Converters.Add(new Web.Common.DddJsonConverter());
                options.JsonSerializerOptions.Converters.Add(new Web.Common.DdiJsonConverter());
                options.JsonSerializerOptions.Converters.Add(new Web.Common.DocumentoJsonConverter());
                options.JsonSerializerOptions.Converters.Add(new Web.Common.CpfJsonConverter());
                options.JsonSerializerOptions.Converters.Add(new Web.Common.CnpjJsonConverter());
                options.JsonSerializerOptions.Converters.Add(new Web.Common.DocumentoGenericoJsonConverter());
            });

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

                        return new Common.Results.ResultadoErro(
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

        builder.Services.AddOpenApiDocument(options =>
        {
            options.Title = "Projeto Sistemas";
            options.DocumentProcessors.Add(new Web.Controllers.Catalogo.CircularReferenceDocumentProcessor());
            options.SchemaSettings.TypeMappers.Add(new NJsonSchema.Generation.TypeMappers.PrimitiveTypeMapper(typeof(Common.ValueObjects.Ddd), schema => schema.Type = NJsonSchema.JsonObjectType.String));
            options.SchemaSettings.TypeMappers.Add(new NJsonSchema.Generation.TypeMappers.PrimitiveTypeMapper(typeof(Common.ValueObjects.Ddi), schema => schema.Type = NJsonSchema.JsonObjectType.String));
            options.SchemaSettings.TypeMappers.Add(new NJsonSchema.Generation.TypeMappers.PrimitiveTypeMapper(typeof(Common.ValueObjects.Documento), schema => schema.Type = NJsonSchema.JsonObjectType.String));
            options.SchemaSettings.TypeMappers.Add(new NJsonSchema.Generation.TypeMappers.PrimitiveTypeMapper(typeof(Common.ValueObjects.Cpf), schema => schema.Type = NJsonSchema.JsonObjectType.String));
            options.SchemaSettings.TypeMappers.Add(new NJsonSchema.Generation.TypeMappers.PrimitiveTypeMapper(typeof(Common.ValueObjects.Cnpj), schema => schema.Type = NJsonSchema.JsonObjectType.String));
            options.SchemaSettings.TypeMappers.Add(new NJsonSchema.Generation.TypeMappers.PrimitiveTypeMapper(typeof(Common.ValueObjects.DocumentoGenerico), schema => schema.Type = NJsonSchema.JsonObjectType.String));
        });

        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("A Connection String 'DefaultConnection' não foi encontrada no appsettings.json.");

        builder.Services.AddPostgreSQLInfrastructure(connectionString);

        var app = builder.Build();

        app.UseMiddleware<Web.Middlewares.GlobalExceptionMiddleware>();

        app.UseCors();

        app.UseOpenApi();

        app.UseSwaggerUi(config =>
        {
            config.Path = string.Empty;
        });

        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}