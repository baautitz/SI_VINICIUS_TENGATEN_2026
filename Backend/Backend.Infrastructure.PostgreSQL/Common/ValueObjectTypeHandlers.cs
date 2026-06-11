using System;
using System.Data;
using System.Linq;
using Backend.Core.Common.ValueObjects;
using Dapper;

namespace Backend.Infrastructure.PostgreSQL.Common;

public class DddTypeHandler : SqlMapper.TypeHandler<Ddd>
{
    public override void SetValue(IDbDataParameter parameter, Ddd value)
    {
        parameter.Value = value.Valor ?? string.Empty;
    }

    public override Ddd Parse(object value)
    {
        return new Ddd(value?.ToString() ?? string.Empty);
    }
}

public class DdiTypeHandler : SqlMapper.TypeHandler<Ddi>
{
    public override void SetValue(IDbDataParameter parameter, Ddi value)
    {
        parameter.Value = value.Valor ?? string.Empty;
    }

    public override Ddi Parse(object value)
    {
        return new Ddi(value?.ToString() ?? string.Empty);
    }
}

public class DocumentoTypeHandler : SqlMapper.TypeHandler<Documento>
{
    public override void SetValue(IDbDataParameter parameter, Documento? value)
    {
        parameter.Value = value?.Valor ?? (object)DBNull.Value;
    }

    public override Documento? Parse(object? value)
    {
        if (value == null || value == DBNull.Value)
            return null;

        var stringValue = value.ToString() ?? string.Empty;
        var cleanValue = new string(stringValue.Where(char.IsLetterOrDigit).ToArray());

        if (cleanValue.Length == 11)
            return new Cpf(stringValue);
        if (cleanValue.Length == 14)
            return new Cnpj(stringValue);

        return new DocumentoGenerico(stringValue);
    }
}
