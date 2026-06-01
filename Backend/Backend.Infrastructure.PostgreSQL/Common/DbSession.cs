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
        using var cmd = Connection.CreateCommand();
        cmd.CommandText = "SET search_path TO projeto_sistemas, public;";
        cmd.ExecuteNonQuery();
    }

    public void Dispose()
    {
        Connection?.Dispose();
    }
}