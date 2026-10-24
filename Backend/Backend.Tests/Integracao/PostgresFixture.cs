using Backend.Infrastructure.PostgreSQL.Common;
using Dapper;
using Testcontainers.PostgreSql;

namespace Backend.Tests.Integracao;

// Sobe um Postgres descartável e carrega o schema real (infrastructure/database/00_master.sql).
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine").Build();

    static PostgresFixture()
    {
        DefaultTypeMap.MatchNamesWithUnderscores = true;
        SqlMapper.AddTypeHandler(new DddTypeHandler());
        SqlMapper.AddTypeHandler(new DdiTypeHandler());
        SqlMapper.AddTypeHandler(new DocumentoTypeHandler());
    }

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        await _container.CopyAsync(LocalizarPastaDatabase(), "/tmp/database");
        var r = await _container.ExecAsync(["psql", "-U", "postgres", "-d", "postgres", "-v", "ON_ERROR_STOP=1", "-q", "-f", "/tmp/database/00_master.sql"]);
        if (r.ExitCode != 0) throw new InvalidOperationException($"Falha ao carregar o schema: {r.Stderr}");
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    public DbSession NovaSessao() => new(ConnectionString);

    public Task Limpar() => Executar("TRUNCATE contas_pagar, contas_receber, fornecedores, clientes, transportadoras RESTART IDENTITY CASCADE;");

    public async Task Executar(string sql, object? param = null)
    {
        using var s = NovaSessao();
        await s.Connection.ExecuteAsync(sql, param);
    }

    private static DirectoryInfo LocalizarPastaDatabase()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "infrastructure", "database"))) dir = dir.Parent;
        return dir == null ? throw new DirectoryNotFoundException("infrastructure/database não encontrado.") : new DirectoryInfo(Path.Combine(dir.FullName, "infrastructure", "database"));
    }
}

[CollectionDefinition(Nome)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Nome = "postgres";
}
