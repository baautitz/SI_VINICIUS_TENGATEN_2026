using System;
using Backend.Core.Common.Exceptions;
using Npgsql;

namespace Backend.Infrastructure.PostgreSQL.Common;

public static class DbExceptionTranslator
{
    public static Exception Translate(PostgresException ex)
    {
        return ex.SqlState switch
        {
            "23505" => new UniqueConstraintException("Este registro já existe.", ex),
            "23503" => new ConflictException("Este registro não pode ser alterado ou excluído pois está sendo utilizado em outro lugar.", ex),
            _ => ex
        };
    }
}
