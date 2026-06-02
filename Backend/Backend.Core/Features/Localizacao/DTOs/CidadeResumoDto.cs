using Backend.Core.Common.ValueObjects;

namespace Backend.Core.Features.Localizacao.DTOs;

public record CidadeResumoDto(
    int Id,
    string Cidade,
    short Ddd,
    int EstadoId,
    string EstadoNome,
    string Uf
);