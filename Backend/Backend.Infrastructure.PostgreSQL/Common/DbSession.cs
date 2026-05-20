using System.Data;
using Npgsql;

namespace Backend.Infrastructure.PostgreSQL.Common;

public sealed class DbSession : IDisposable
{
    public IDbConnection Connection { get; }
    public IDbTransaction? Transaction { get; set; }

    public DbSession(string connectionString)
    {
        Connection = new NpgsqlConnection(connectionString);
        Connection.Open();
    }

    public void Dispose()
    {
        Connection?.Dispose();
    }
}