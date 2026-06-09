namespace Backend.Core.Features.Localizacao.Commands;

public record CriarEstadoCommand(string Estado, string Uf, int PaisId);
public record AtualizarEstadoCommand(string Estado, string Uf, int PaisId);
