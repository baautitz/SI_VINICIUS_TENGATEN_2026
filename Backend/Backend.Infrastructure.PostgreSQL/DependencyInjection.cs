using Backend.Core.Common;
using Backend.Infrastructure.PostgreSQL.Common;
using Dapper;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.Infrastructure.PostgreSQL;

public static class DependencyInjection
{
    public static IServiceCollection AddPostgreSQLInfrastructure(this IServiceCollection services, string connectionString)
    {
        DefaultTypeMap.MatchNamesWithUnderscores = true;

        services.AddScoped(provider => new DbSession(connectionString));
        services.AddScoped<IUnitOfWork, PostgreSQLUnitOfWork>();

        //services.AddScoped<IPaisRepository, PaisRepository>();

        return services;
    }
}