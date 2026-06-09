namespace Backend.Core.Features.Localizacao.Commands;

public record CriarCidadeCommand(string Cidade, string Ddd, int EstadoId);
public record AtualizarCidadeCommand(string Cidade, string Ddd, int EstadoId);
