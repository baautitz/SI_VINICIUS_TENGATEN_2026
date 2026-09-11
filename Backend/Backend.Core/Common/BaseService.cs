using Backend.Core.Common.Exceptions;
using Backend.Core.Common.Results;

namespace Backend.Core.Common;

public abstract class BaseService
{
    protected async Task<Resultado<T>> ExecuteResultAsync<T>(Func<Task<Resultado<T>>> action)
    {
        try
        {
            return await action();
        }
        catch (UniqueConstraintException ex)
        {
            return Resultado<T>.Falha(new ResultadoErro("DUPLICIDADE", ex.Message));
        }
        catch (ReferencedRecordException ex)
        {
            return Resultado<T>.Falha(new ResultadoErro(ex.Code, ex.Message));
        }
    }
}
