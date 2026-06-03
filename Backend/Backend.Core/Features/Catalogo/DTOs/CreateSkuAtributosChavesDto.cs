using System.Collections.Generic;

namespace Backend.Core.Features.Catalogo.DTOs;

public record CreateSkuAtributosChavesDto(
    string Chave,
    List<string> Valores
);
