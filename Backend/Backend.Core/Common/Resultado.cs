namespace Backend.Core.Common;

public sealed record ResultadoErro(string Code, string Message, string? Field = null);

public sealed class Resultado
{
  public bool Success { get; }
  public IReadOnlyCollection<ResultadoErro>? Errors { get; }

  private Resultado(bool success, IReadOnlyCollection<ResultadoErro>? errors)
  {
    Success = success;
    Errors = errors;
  }

  public static Resultado Sucesso()
      => new Resultado(true, null);

  public static Resultado Falha(params ResultadoErro[] errors)
      => new Resultado(false, errors.Length > 0 ? errors : null);

  public static Resultado Falha(IEnumerable<ResultadoErro> errors)
      => new Resultado(false, errors?.ToArray() ?? Array.Empty<ResultadoErro>());
}

public sealed class Resultado<T>
{
  public bool Success { get; }
  public T? Data { get; }
  public IReadOnlyCollection<ResultadoErro>? Errors { get; }

  private Resultado(T? data, bool success, IReadOnlyCollection<ResultadoErro>? errors)
  {
    Success = success;
    Data = data;
    Errors = errors;
  }

  public static Resultado<T> Sucesso(T data)
      => new Resultado<T>(data, true, null);

  public static Resultado<T> Falha(params ResultadoErro[] errors)
      => new Resultado<T>(default, false, errors.Length > 0 ? errors : null);

  public static Resultado<T> Falha(IEnumerable<ResultadoErro> errors)
      => new Resultado<T>(default, false, errors?.ToArray() ?? Array.Empty<ResultadoErro>());
}
