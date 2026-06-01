namespace Backend.Core.Features.Localizacao.DTOs;

public record BairroResumoDto(
    int Id,
    string Bairro,
    int CidadeId,
    string CidadeNome,
    string Uf
);