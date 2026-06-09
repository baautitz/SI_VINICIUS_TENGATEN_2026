using System.Collections.Generic;
namespace Backend.Core.Features.Catalogo.Commands;

public record CriarSkuAtributosChavesCommand(string Chave, List<string> Valores);
public record AtualizarSkuAtributosChavesCommand(string Chave, List<string> Valores);
