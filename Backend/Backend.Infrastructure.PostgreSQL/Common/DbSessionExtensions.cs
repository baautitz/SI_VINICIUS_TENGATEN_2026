using Backend.Core.Common.Exceptions;
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
        catch (PostgresException ex) when (ex.SqlState == "23505") // Unique Violation
        {
            throw new UniqueConstraintException("Este registro já existe.", ex);
        }
        catch (PostgresException ex) when (ex.SqlState == "23503") // Foreign Key Violation
        {
            throw new ConflictException("Este registro não pode ser alterado ou excluído pois está sendo utilizado em outro lugar.", ex);
        }
    }
}
