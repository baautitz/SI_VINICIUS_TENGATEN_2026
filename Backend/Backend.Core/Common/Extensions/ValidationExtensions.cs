using System.Collections.Generic;
using System.Linq;
using Backend.Core.Common.Results;
using FluentValidation.Results;

namespace Backend.Core.Common.Extensions;

public static class ValidationExtensions
{
    public static IEnumerable<ResultadoErro> ToResultadoErros(this ValidationResult result)
    {
        return result.Errors.Select(e => new ResultadoErro(
            e.ErrorCode ?? "VALIDACAO",
            e.ErrorMessage,
            e.PropertyName
        ));
    }
}
