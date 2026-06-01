using Backend.Core.Common;
using Npgsql;

namespace Backend.Infrastructure.PostgreSQL.Common;

public static class DbSessionExtensions
{
    public static async Task<T> ExecuteWithConflictCheckAsync<T>(Func<Task<T>> action)
    {
        try
        {
            return await action();
        }
        catch (PostgresException ex) when (ex.SqlState == "23505")
        {
            throw new UniqueConstraintException("Este registro já existe.", ex);
        }
    }
}
