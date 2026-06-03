using System.Collections.Generic;

namespace Backend.Core.Features.Catalogo.DTOs;

public record UpdateSkuAtributosChavesDto(
    string Chave,
    List<string> Valores
);
