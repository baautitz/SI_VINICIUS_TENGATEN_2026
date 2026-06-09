namespace Backend.Core.Features.Localizacao.Commands;

public record CriarBairroCommand(string Bairro, int CidadeId);
public record AtualizarBairroCommand(string Bairro, int CidadeId);
