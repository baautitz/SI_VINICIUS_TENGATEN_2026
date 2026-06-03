using System.Collections.Generic;

namespace Backend.Core.Features.Catalogo.DTOs;

public record SkuAtributosChavesResumo(
    int Id,
    string Chave,
    List<string> Valores
);
