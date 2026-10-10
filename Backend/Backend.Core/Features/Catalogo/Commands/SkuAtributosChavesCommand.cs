using System.Collections.Generic;
namespace Backend.Core.Features.Catalogo.Commands;

/// <summary>Valor de atributo: com Id é um valor existente (renomear mantém os vínculos com SKUs); sem Id é novo.</summary>
public record AtributoValorItem(int? Id, string Valor);

public record CriarSkuAtributosChavesCommand(string Chave, List<AtributoValorItem> Valores);
public record AtualizarSkuAtributosChavesCommand(string Chave, List<AtributoValorItem> Valores);
