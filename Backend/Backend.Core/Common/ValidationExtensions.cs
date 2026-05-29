using FluentValidation.Results;

namespace Backend.Core.Common;

public static class ValidationExtensions
{
  public static IReadOnlyCollection<ResultadoErro> ToResultadoErros(this ValidationResult result)
      => result.Errors
          .Select(error => new ResultadoErro(
              Code: !string.IsNullOrWhiteSpace(error.ErrorCode)
                  ? error.ErrorCode
                  : ToUpperSnakeCase(error.PropertyName),
              Message: error.ErrorMessage,
              Field: string.IsNullOrWhiteSpace(error.PropertyName) ? null : error.PropertyName
          ))
          .ToArray();

  public static Resultado ToResultado(this ValidationResult result)
      => result.IsValid
          ? Resultado.Sucesso()
          : Resultado.Falha(result.ToResultadoErros());

  public static Resultado<T> ToResultado<T>(this ValidationResult result, T data)
      => result.IsValid
          ? Resultado<T>.Sucesso(data)
          : Resultado<T>.Falha(result.ToResultadoErros());

  private static string ToUpperSnakeCase(string text)
  {
    if (string.IsNullOrWhiteSpace(text))
      return "VALIDACAO_INVALIDA";

    var builder = new System.Text.StringBuilder();
    foreach (var ch in text)
    {
      if (char.IsUpper(ch) && builder.Length > 0)
        builder.Append('_');

      builder.Append(char.ToUpperInvariant(ch));
    }

    return builder.ToString();
  }
}
