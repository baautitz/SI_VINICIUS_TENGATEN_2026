namespace Backend.Core.Features.Catalogo.Commands;

public record CriarUnidadeMedidaCommand(string Sigla, string Descricao, string Categoria, bool PermiteDecimais, bool Ativo);
public record AtualizarUnidadeMedidaCommand(string Sigla, string Descricao, string Categoria, bool PermiteDecimais, bool Ativo);
